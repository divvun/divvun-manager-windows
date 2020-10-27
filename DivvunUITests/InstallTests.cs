using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Remote;
using System;
using System.Threading;
using Xunit;

namespace DivvunUITest
{
    public class InstallTests
    {
        [Fact]
        public void TestInstall()
        {
            var appiumOptions = new OpenQA.Selenium.Appium.AppiumOptions();
            appiumOptions.AddAdditionalCapability("app", @"F:\Dev\divvun-manager-windows\Divvun.Installer\bin\x86\Release\DivvunInstaller.exe");
            var divvunSession = new WindowsDriver<WindowsElement>(new Uri("http://127.0.0.1:4723"), appiumOptions);

            divvunSession.SwitchTo().Window(divvunSession.WindowHandles[0]);
            divvunSession.FindElementByAccessibilityId("TitleBarReposButton").SendKeys(Keys.Return);
            divvunSession.FindElementByName("All Repositories").SendKeys(Keys.Return);

            var treeItem = divvunSession.FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3];
            treeItem.SendKeys(Keys.Space);

            divvunSession.FindElementByAccessibilityId("BtnPrimary").SendKeys(Keys.Enter);

            Thread.Sleep(20000);
            divvunSession.FindElementByAccessibilityId("BtnFinish").SendKeys(Keys.Enter);

            var installedText = divvunSession
                .FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3]
                .FindElementsByName("Divvun.Installer.UI.Shared.PackageMenuItem")[0]
                .FindElementsByClassName("TextBlock")[3].Text;

            Assert.Equal("Installed", installedText);
        }

    }
}
