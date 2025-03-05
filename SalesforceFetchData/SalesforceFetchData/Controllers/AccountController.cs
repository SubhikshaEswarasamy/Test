using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AccountController : Controller
{
    private readonly SalesforceService _salesforceService;

    public AccountController(SalesforceService salesforceService) => _salesforceService = salesforceService;

    public async Task<IActionResult> Index()
    {
        List<Dictionary<string, object>> accounts = await _salesforceService.FetchAccountRecords();
        return View(accounts);
    }
}