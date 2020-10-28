using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Interfaces;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Remote;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
            try
            {
                Thread.Sleep(20000);
                this.Session.FindElementByAccessibilityId("TitleBarReposButton").SendKeys(Keys.Return);
                this.Session.FindElementByName("All Repositories").SendKeys(Keys.Return);

                Thread.Sleep(20000);
                var treeItem = this.Session.FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3];
                treeItem.SendKeys(Keys.Space);
                Thread.Sleep(20000);
                this.Session.FindElementByAccessibilityId("BtnPrimary").SendKeys(Keys.Enter);

                Thread.Sleep(30000);
                this.Session.FindElementByAccessibilityId("BtnFinish").SendKeys(Keys.Enter);

                Thread.Sleep(5000);
                var installedText = this.Session
                    .FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3]
                    .FindElementsByName("Divvun.Installer.UI.Shared.PackageMenuItem")[0]
                    .FindElementsByClassName("TextBlock")[3].Text;

                Assert.Equal("Installed", installedText);
            }
            catch
            {
                this.SaveScreenshot();
                throw;
            }
        }

        [Fact]
        [TestPriority(2)]
        public void TestUninstall()
        {
            try
            {
                Thread.Sleep(20000);
                this.Session.FindElementByAccessibilityId("TitleBarReposButton").SendKeys(Keys.Return);
                this.Session.FindElementByName("All Repositories").SendKeys(Keys.Return);

                Thread.Sleep(20000);
                var treeItem = this.Session.FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3];
                treeItem.SendKeys(Keys.Space);

                Thread.Sleep(20000);
                this.Session.FindElementByAccessibilityId("BtnPrimary").SendKeys(Keys.Enter);

                Thread.Sleep(20000);
                this.Session.FindElementByAccessibilityId("BtnFinish").SendKeys(Keys.Enter);

                Thread.Sleep(5000);
                var installedText = this.Session
                    .FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3]
                    .FindElementsByName("Divvun.Installer.UI.Shared.PackageMenuItem")[0]
                    .FindElementsByClassName("TextBlock")[3].Text;

                Assert.Equal("Not Installed", installedText);
            }
            catch
            {
                this.SaveScreenshot();
                throw;
            }
        }
    }
}
