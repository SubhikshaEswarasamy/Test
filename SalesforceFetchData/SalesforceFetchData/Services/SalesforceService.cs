using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;

public class SalesforceService
{
    private readonly SalesforceAuthService _authService;
    private readonly string _instanceUrl;
    private readonly string _queryTemplate;
    private readonly Dictionary<string, string> _fieldMappings;

    public SalesforceService(SalesforceAuthService authService, IConfiguration config)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _instanceUrl = config["Salesforce:InstanceUrl"] ?? throw new Exception("Salesforce InstanceUrl is not configured.");
        _queryTemplate = config["Salesforce:Queries:AccountQuery"] ?? throw new Exception("Salesforce AccountQuery is not configured.");

        _fieldMappings = config.GetSection("ApiMappings").Get<Dictionary<string, string>>() ?? throw new Exception("ApiMappings is missing or empty in appsettings.json.");
    }

    public async Task<List<Dictionary<string, object>>> FetchAccountRecords()
    {
        try
        {
            string accessToken = await _authService.GetAccessToken();
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var selectedFields = _fieldMappings.Keys.ToList();
            if (!selectedFields.Any())
            {
                throw new Exception("No fields specified for the Salesforce query.");
            }

            string dynamicQuery = _queryTemplate.Replace("{fields}", string.Join(", ", selectedFields));
            Console.WriteLine($"Generated SOQL Query: {dynamicQuery}");

            var response = await client.GetAsync($"{_instanceUrl}/services/data/v59.0/query/?q={Uri.EscapeDataString(dynamicQuery)}");

            if (!response.IsSuccessStatusCode)
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
                throw new Exception($"Salesforce API error: {response.StatusCode} - {errorResponse}");
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JToken parsedJson = JToken.Parse(jsonResponse);

            if (parsedJson["records"] == null)
            {
                throw new Exception("Unexpected JSON format from Salesforce API. No 'records' found.");
            }

            return parsedJson["records"].ToObject<List<Dictionary<string, object>>>().Select(MapFields).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching account records: {ex.Message}");
            return new List<Dictionary<string, object>>();
        }
    }

    private Dictionary<string, object> MapFields(Dictionary<string, object> record)
    {
        var mappedRecord = new Dictionary<string, object>();
        try
        {
            foreach (var kvp in record)
            {
                if (kvp.Key == "attributes") continue;
                string mappedKey = _fieldMappings.TryGetValue(kvp.Key, out var mappedValue) ? mappedValue : kvp.Key;
                mappedRecord[mappedKey] = kvp.Value ?? "";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error mapping fields: {ex.Message}");
        }
        return mappedRecord;
    }

    public async Task<byte[]> ExportToExcel()
    {
        try
        {
            var records = await FetchAccountRecords();
            if (records == null || !records.Any())
            {
                throw new Exception("No records found to export.");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Accounts");

            var headers = records.First().Keys.ToList();
            for (int col = 0; col < headers.Count; col++)
            {
                worksheet.Cells[1, col + 1].Value = headers[col];
                worksheet.Cells[1, col + 1].Style.Font.Bold = true;
            }

            for (int row = 0; row < records.Count; row++)
            {
                for (int col = 0; col < headers.Count; col++)
                {
                    worksheet.Cells[row + 2, col + 1].Value = records[row].ContainsKey(headers[col])
                        ? records[row][headers[col]]
                        : "";
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting to Excel: {ex.Message}");
            return Array.Empty<byte>();
        }
    }
}
