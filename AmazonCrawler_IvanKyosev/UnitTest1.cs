using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;

[TestFixture]
public class AmazonCrawlerTests
{
    private IWebDriver driver;

    [SetUp]
    public void Setup()
    {
        driver = new ChromeDriver();
        driver.Manage().Window.Maximize();
    }

    [TearDown]
    public void Teardown()
    {
        driver.Quit();
        driver.Dispose();
    }

    [Test]
    public void RunCrawler()
    {
        driver.Navigate().GoToUrl("https://www.amazon.com");

        IWebElement shopByDepartment = driver.FindElement(By.Id("nav-hamburger-menu"));
        shopByDepartment.Click();

        List<string> departmentLinks = GetDepartmentLinks();

        List<LinkStatus> linkStatuses = new List<LinkStatus>();

        foreach (string departmentLink in departmentLinks)
        {
            driver.Navigate().GoToUrl(departmentLink);

            string pageTitle = driver.Title;
            HttpStatusCode status = GetResponseStatus(departmentLink);

            linkStatuses.Add(new LinkStatus(departmentLink, pageTitle, status));
        }

        SaveResultsToFile(linkStatuses);
    }

    private List<string> GetDepartmentLinks()
    {
        List<string> departmentLinks = new List<string>();

        IReadOnlyCollection<IWebElement> departmentElements = driver.FindElements(By.CssSelector(".hmenu-item:not(.hmenu-back-button) a"));

        foreach (IWebElement departmentElement in departmentElements)
        {
            departmentLinks.Add(departmentElement.GetAttribute("href"));
        }

        return departmentLinks;
    }

    private HttpStatusCode GetResponseStatus(string url)
    {
        HttpStatusCode statusCode;

        using (HttpClient client = new HttpClient())
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, url);
            HttpResponseMessage response = client.SendAsync(request).Result;
            statusCode = response.StatusCode;
        }

        return statusCode;
    }

    private void SaveResultsToFile(List<LinkStatus> linkStatuses)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        string filename = $"{timestamp}_results.txt";

        using (StreamWriter writer = new StreamWriter(filename))
        {
            foreach (LinkStatus linkStatus in linkStatuses)
            {
                string status = (linkStatus.Status == HttpStatusCode.OK) ? "OK" : "Dead link";
                writer.WriteLine($"{linkStatus.Link},{linkStatus.PageTitle},{status}");
            }
        }
    }

    private class LinkStatus
    {
        public string Link { get; }
        public string PageTitle { get; }
        public HttpStatusCode Status { get; }

        public LinkStatus(string link, string pageTitle, HttpStatusCode status)
        {
            Link = link;
            PageTitle = pageTitle;
            Status = status;
        }
    }
}