using System.Threading;
using OpenQA.Selenium;
using Xunit;

namespace DivvunUITests {

[TestCaseOrderer("DivvunUITests.PriorityOrderer", "DivvunUITests")]
public class InstallTests : DivvunSession {
    [Fact]
    [TestPriority(1)]
    public void TestInstall() {
        try {
            Thread.Sleep(20000);
            Session.FindElementByAccessibilityId("TitleBarReposButton").SendKeys(Keys.Return);
            Session.FindElementByName("All Repositories").SendKeys(Keys.Return);

            Thread.Sleep(20000);
            var treeItem = Session.FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3];
            treeItem.SendKeys(Keys.Space);
            Thread.Sleep(20000);
            Session.FindElementByAccessibilityId("BtnPrimary").SendKeys(Keys.Enter);

            Thread.Sleep(30000);
            Session.FindElementByAccessibilityId("BtnFinish").SendKeys(Keys.Enter);

            Thread.Sleep(5000);
            var installedText = Session
                .FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3]
                .FindElementsByName("Divvun.Installer.UI.Shared.PackageMenuItem")[0]
                .FindElementsByClassName("TextBlock")[3].Text;

            Assert.Equal("Installed", installedText);
        }
        catch {
            SaveScreenshot();
            throw;
        }
    }

    [Fact]
    [TestPriority(2)]
    public void TestUninstall() {
        try {
            Thread.Sleep(20000);
            Session.FindElementByAccessibilityId("TitleBarReposButton").SendKeys(Keys.Return);
            Session.FindElementByName("All Repositories").SendKeys(Keys.Return);

            Thread.Sleep(20000);
            var treeItem = Session.FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3];
            treeItem.SendKeys(Keys.Space);

            Thread.Sleep(20000);
            Session.FindElementByAccessibilityId("BtnPrimary").SendKeys(Keys.Enter);

            Thread.Sleep(20000);
            Session.FindElementByAccessibilityId("BtnFinish").SendKeys(Keys.Enter);

            Thread.Sleep(5000);
            var installedText = Session
                .FindElementsByName("Divvun.Installer.UI.Shared.PackageCategoryTreeItem")[3]
                .FindElementsByName("Divvun.Installer.UI.Shared.PackageMenuItem")[0]
                .FindElementsByClassName("TextBlock")[3].Text;

            Assert.Equal("Not Installed", installedText);
        }
        catch {
            SaveScreenshot();
            throw;
        }
    }
}

}