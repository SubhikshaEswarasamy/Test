using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

[Route("api/salesforce")]
[ApiController]
public class SalesforceController : Controller
{
    private readonly SalesforceService _salesforceService;

    public SalesforceController(SalesforceService salesforceService)
    {
        _salesforceService = salesforceService;
    }


    [HttpGet("export")]
    public async Task<IActionResult> ExportExcel()
    {
        try
        {
            var excelFile = await _salesforceService.ExportToExcel();

            return File(
                excelFile,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Salesforce_Accounts.xlsx"
            );
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error exporting data: {ex.Message}");
        }
    }
}
