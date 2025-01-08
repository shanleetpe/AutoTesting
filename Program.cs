// See https://aka.ms/new-console-template for more information

using System.Configuration;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Playwright;
using Microsoft.Win32;
using HtmlAgilityPack;
using System.Text;
using log4net;
using log4net.Config;
using log4net.Repository;
using System.Reflection;

// 2024/03/12 用異步作自動測試
//var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
var _log4net = log4net.LogManager.GetLogger(typeof(Program));
XmlConfigurator.Configure();

FreeConsole();
//錄製命令
//cd D:\tim_hsu\Downloads\SimulateMouseMove\ConsoleApp5\
//pwsh bin\Debug\net7.0-windows\playwright.ps1 codegen demo.playwright.dev/todomvc
int mock_Test_Waitforclick = 3000;
int.TryParse(ConfigurationManager.AppSettings["mock_Test_Waitforclick"], out mock_Test_Waitforclick);
int mock_Test_waitforSelect = 2000;

int mock_Test_Waitforpagechange = 3000;
int.TryParse(ConfigurationManager.AppSettings["mock_Test_Waitforpagechange"], out mock_Test_Waitforpagechange);

int formal_Test_Waitforclick = 6000;
int.TryParse(ConfigurationManager.AppSettings["formal_Test_Waitforclick"], out formal_Test_Waitforclick);

int formal_Test_Waitforpagechange = 9000;
int.TryParse(ConfigurationManager.AppSettings["formal_Test_Waitforpagechange"], out formal_Test_Waitforpagechange);

int SlowMo = 1000;
int.TryParse(ConfigurationManager.AppSettings["SlowMo"], out SlowMo);

int PageTimeOut = 30000;
int.TryParse(ConfigurationManager.AppSettings["PageTimeOut"], out PageTimeOut);

//var exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });
//if (exitCode != 0)
//{
//    Console.WriteLine("Failed to install browsers");
//    Environment.Exit(exitCode);
//}

//Console.WriteLine("Browsers installed");
using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Args = new string[] { "--start-maximized" },
    Headless = false,
    ExecutablePath = GetChromePath(),
    SlowMo = SlowMo

}); ;
var context = await browser.NewContextAsync(new BrowserNewContextOptions
{
    ViewportSize = ViewportSize.NoViewport
});
var page = await context.NewPageAsync();
//This setting will change the default maximum time for all the methods accepting timeout option 
//page.SetDefaultTimeout(60000);
var url = ConfigurationManager.AppSettings["Url"];
////20240308 增加網頁控制,統一執行
//var ctrlPanel_url = ConfigurationManager.AppSettings["Ctrl_Url"];
//HtmlWeb htmlWeb = new HtmlWeb();
//htmlWeb.OverrideEncoding = Encoding.UTF8;
//HtmlDocument doc = htmlWeb.Load(ctrlPanel_url);
//HtmlNode node = doc.DocumentNode.SelectSingleNode("//input[@id='BTN_Login']");

////await page.GotoAsync(ctrlPanel_url);
//var start = node.GetAttributeValue("value","");
//while (start == "啟動")
//{
//    Thread.Sleep(3000);
//    doc = htmlWeb.Load(ctrlPanel_url);
//    node = doc.DocumentNode.SelectSingleNode("//input[@id='BTN_Login']");

//    //await page.GotoAsync(ctrlPanel_url);
//    start = node.GetAttributeValue("value", "");
//}

//url = null;
try
{
    if (string.IsNullOrEmpty(url))
        throw new ApplicationException("Url is null");
    else
    {
        await page.GotoAsync(url);
        await page.WaitForTimeoutAsync(mock_Test_Waitforclick);
        await page.ClickAsync("select[name='M_EXAMGRID']", new PageClickOptions { Timeout = mock_Test_Waitforpagechange });
        var joblevel = await page.QuerySelectorAsync("select[name='M_EXAMGRID']");
        if (joblevel != null)
        {
            await joblevel.PressAsync("ArrowDown");
            await page.WaitForTimeoutAsync(mock_Test_waitforSelect);
            await joblevel.PressAsync("ArrowDown");
            await page.WaitForTimeoutAsync(mock_Test_waitforSelect);
            await joblevel.PressAsync("ArrowDown");
            await page.WaitForTimeoutAsync(mock_Test_waitforSelect);
            //await joblevel.PressAsync("ArrowUp");
            //await page.WaitForTimeoutAsync(mock_Test_waitforSelect);
            //await joblevel.PressAsync("ArrowUp");
            //await page.WaitForTimeoutAsync(mock_Test_waitforSelect);
        }

        await page.WaitForTimeoutAsync(mock_Test_Waitforclick);
        var popup = await page.RunAndWaitForPopupAsync(async () =>
        {
            await page.WaitForTimeoutAsync(mock_Test_Waitforclick);
            await page.ClickAsync("input[id='BTN_ENTER']");
        });
        await popup.WaitForLoadStateAsync(LoadState.Load);
        await popup.WaitForTimeoutAsync(mock_Test_Waitforclick);
        //模測部份
        var NumofQuection = await popup.InnerTextAsync("span[id='M_CNT']");
        if (NumofQuection == null)
        {
            Console.WriteLine("模測部份題數錯誤");
            return;
        }
        int num = 6;
        int.TryParse(NumofQuection, out num);
        for (int i = 0; i < num; i++)
        {
            await nextQuection(popup, mock_Test_Waitforclick, mock_Test_Waitforpagechange, PageTimeOut, getIntRnd(2));
        }

        await popup.WaitForTimeoutAsync(mock_Test_Waitforclick);
        await popup.GetByRole(AriaRole.Button, new() { Name = "提前結束模擬考試" }).ClickAsync();
        await popup.WaitForTimeoutAsync(3000);
        //await popup.GetByRole(AriaRole.Button, new() { Name = "確定" }).ClickAsync();
        //

        //正式測試部份
        NumofQuection = await popup.InnerTextAsync("span[id='M_CNT']");
        if (NumofQuection == null)
        {
            Console.WriteLine("正式測試題數錯誤");
            return;
        }
        num = 80;
        int.TryParse(NumofQuection, out num);
        for (int i = 0; i < num; i++)
        {
            await nextQuection(popup, formal_Test_Waitforclick, formal_Test_Waitforpagechange, PageTimeOut, getIntRnd(4));
        }

        await popup.GetByRole(AriaRole.Button, new() { Name = "提前結束" }).ClickAsync();
        await popup.WaitForTimeoutAsync(formal_Test_Waitforclick);
        await popup.GetByRole(AriaRole.Button, new() { Name = "確定" }).ClickAsync();
        await popup.WaitForTimeoutAsync(formal_Test_Waitforclick);
        await popup.GetByRole(AriaRole.Button, new() { Name = "確定" }).ClickAsync();
        await popup.WaitForTimeoutAsync(formal_Test_Waitforclick);
        await popup.GetByRole(AriaRole.Button, new() { Name = "確定" }).ClickAsync();
        await popup.WaitForTimeoutAsync(formal_Test_Waitforclick);

        var detailsModal = popup.GetByRole(AriaRole.Button, new() { Name = "確定" });
        await detailsModal.WaitForAsync(new LocatorWaitForOptions()
        {
            State = WaitForSelectorState.Visible,
            Timeout = PageTimeOut,
        });
        await detailsModal.ClickAsync();
        //await popup.GetByRole(AriaRole.Button, new() { Name = "確定" }).ClickAsync();
        await popup.WaitForTimeoutAsync(formal_Test_Waitforclick);
        await popup.ScreenshotAsync(new()
        {
            Path = "screenshot.png"
        });
        Thread.Sleep(3000);
        await popup.CloseAsync();
        await page.CloseAsync();
        await context.CloseAsync();
        await browser.CloseAsync();
    }

}
catch (Exception ex)
{
    _log4net.Error("<br/>" + ex.Message.ToString() + "<br/>");
    //Console.WriteLine(ex.Message);
}

[System.Runtime.InteropServices.DllImport("kernel32.dll")]
static extern bool FreeConsole();


static async Task nextQuection(IPage page, int waitsecconds, int waitforpagechange, int pagetimeout, int rnd)
{
    await page.WaitForTimeoutAsync(waitsecconds);
    var multiple = await page.InnerTextAsync("button[id='M_SM']");
    Random crandom = new Random();
    if (multiple != null && multiple == "是非題")
    {
        rnd = crandom.Next(1, 3);
        //rnd = getIntRnd(2);
    }
    else
    {
        rnd = crandom.Next(1, 5);
    }
    await sel_option(page, rnd);
    await page.WaitForTimeoutAsync(waitforpagechange);
    var detailsModal = page.Locator("#btnNext");
    await detailsModal.WaitForAsync(new LocatorWaitForOptions()
    {
        State = WaitForSelectorState.Visible,
        Timeout = pagetimeout,
    });
    await detailsModal.ClickAsync();
    //await page.Locator("#xNest").WaitForAsync(new LocatorWaitForOptions() { }).ClickAsync();
}

static async Task sel_option(IPage page, int rnd)
{

    //await page1.GetByRole(AriaRole.Row, new() { Name = "(1) 0.1～0.25" }).Locator("[id=\"\\30 1500\"]").PressAsync("ArrowDown");
    switch (rnd)
    {
        case 1:
            await page.Locator("#sel_Option").Locator("input[value='1']").First.CheckAsync();
            break;
        case 2:
            await page.Locator("#sel_Option").Locator("input[value='2']").First.CheckAsync();
            break;
        case 3:
            await page.Locator("#sel_Option").Locator("input[value='3']").First.CheckAsync();
            break;
        case 4:
            await page.Locator("#sel_Option").Locator("input[value='4']").First.CheckAsync();
            break;
        default:
            await page.Locator("#sel_Option").Locator("input[name='radio']").First.CheckAsync();
            break;
    }
}

int getIntRnd(int val)
{
    int result = val;
    Random crandom = new Random();
    result = crandom.Next(1, val + 1);
    return result;
}

// get Chrome installed path from registry (Windows only)
[SupportedOSPlatform("windows")]
string GetChromePath()
{
    var path = Registry.GetValue(@"HKEY_CLASSES_ROOT\ChromeHTML\shell\open\command", null, null) as string;
    if (string.IsNullOrEmpty(path))
        throw new ApplicationException("Chrome not installed");
    var m = Regex.Match(path, "\"(?<p>.+?)\"");
    if (!m.Success)
        throw new ApplicationException($"Invalid Chrome path - {path}");
    return m.Groups["p"].Value;
}
