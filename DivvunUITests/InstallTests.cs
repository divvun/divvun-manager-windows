using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Remote;
using System;
using System.Threading;
using Xunit;

namespace DivvunUITests
{
    [TestCaseOrderer("DivvunUITests.PriorityOrderer", "DivvunUITests")]
    public class InstallTests : DivvunSession
    {
        [Fact]
        [TestPriority(1)]
        public void TestInstall()
        {
            this.Session.FindElementByAccessibilityId("TitleBarReposButton").SendKeys(Keys.Return);
            this.Session.FindElementByName("All Repositories").SendKeys(Keys.Return);

            var treeItem = this.Session.FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3];
            treeItem.SendKeys(Keys.Space);

            this.Session.FindElementByAccessibilityId("BtnPrimary").SendKeys(Keys.Enter);

            Thread.Sleep(20000);
            this.Session.FindElementByAccessibilityId("BtnFinish").SendKeys(Keys.Enter);

            var installedText = this.Session
                .FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3]
                .FindElementsByName("Divvun.Installer.UI.Shared.PackageMenuItem")[0]
                .FindElementsByClassName("TextBlock")[3].Text;

            Assert.Equal("Installed", installedText);
        }

        [Fact]
        [TestPriority(2)]
        public void TestUninstall()
        {
            this.Session.FindElementByAccessibilityId("TitleBarReposButton").SendKeys(Keys.Return);
            this.Session.FindElementByName("All Repositories").SendKeys(Keys.Return);

            var treeItem = this.Session.FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3];
            treeItem.SendKeys(Keys.Space);

            this.Session.FindElementByAccessibilityId("BtnPrimary").SendKeys(Keys.Enter);

            Thread.Sleep(5000);
            this.Session.FindElementByAccessibilityId("BtnFinish").SendKeys(Keys.Enter);

            var installedText = this.Session
                .FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3]
                .FindElementsByName("Divvun.Installer.UI.Shared.PackageMenuItem")[0]
                .FindElementsByClassName("TextBlock")[3].Text;

            Assert.Equal("Not Installed", installedText);
        }
    }
}
