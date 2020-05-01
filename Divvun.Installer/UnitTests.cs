//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reactive.Linq;
//using System.Reactive.Subjects;
//using System.Threading;
//using Divvun.Installer.Models;
//using Divvun.Installer.Models.AppConfigEvent;
//using Divvun.Installer.Properties;
//using Divvun.Installer.Service;
//using Divvun.Installer.UI.Main;
//using Divvun.Installer.UI.Shared;
//using Divvun.Installer.Util;
//using Microsoft.Reactive.Testing;
//using Microsoft.Win32;
//using Moq;
//using Newtonsoft.Json;
//using NUnit.Framework;

//namespace Divvun.Installer
//{
//    public static class ObservableExtensions
//    {
//        public static ITestableObserver<T> Test<T>(this IObservable<T> observable, TestScheduler scheduler)
//        {
//            var testObserver = scheduler.CreateObserver<T>();
//            observable.Subscribe(testObserver);
//            return testObserver;
//        }
//    }

//    [TestFixture]
//    public class UpdaterTests
//    {
//        [SetUp]
//        protected void SetUp()
//        {
            
//        }

//        [Test]
//        public void ConfigContainsDefaultValue()
//        {
//            var registry = new MockRegistry();
//            var store = new AppConfigStore(registry);
//            var scheduler = new TestScheduler();

//            var x = store.State.Test(scheduler);
//            scheduler.Start();
            
//            Assert.AreEqual(Constants.Repository, x.Messages.Last().Value.Value.RepositoryUrl.AbsoluteUri);
//        }
        
//        [Test]
//        public void ConfigUpdatesRepositoryUrlValue()
//        {
//            var registry = new MockRegistry();
//            var store = new AppConfigStore(registry);
//            var scheduler = new TestScheduler();

//            var x = store.State.Test(scheduler);
//            scheduler.Start();
            
//            store.Dispatch(new SetRepositoryUrl(new Uri("https://test.example/")));
//            scheduler.AdvanceBy(1000);
            
//            Assert.AreEqual("https://test.example/", x.Messages.Last().Value.Value.RepositoryUrl.AbsoluteUri);
//            Assert.AreEqual("https://test.example/", registry.LocalMachine
//                .CreateSubKey(@"SOFTWARE\" + Constants.RegistryId)
//                .Get("RepositoryUrl", ""));
//        }

//        [Test]
//        public void UpdaterDetectsNewUpdatesForPackages()
//        {
//            // So this is less fun than anticipated.
//            // Let's see if rx allows selecting on tiem
//        }

//        [Test]
//        public void UpdaterShowsUIWhenUpdatesFound()
//        {
            
//        }

//        [Test]
//        public void UpdaterUsesUserPreferencesForShowingUI()
//        {
            
//        }
//    }

//    [TestFixture]
//    public class UpdateWindowTests
//    {
//        [SetUp]
//        protected void SetUp()
//        {
            
//        }

//        [Test]
//        public void ShowsPackagesToUpdate()
//        {
            
//        }

//        [Test]
//        public void CanSelectPackagesToUpdate()
//        {
            
//        }

//        [Test]
//        public void SelectedPackageShouldShowChangelog()
//        {
            
//        }

//        [Test]
//        public void PressingInstallShouldOpenMainWindowToInstallStep()
//        {
            
//        }

//        [Test]
//        public void IgnoreUpdatesShouldCloseWindow()
//        {
            
//        }
//    }
    
//    [TestFixture]
//    public class MainTests
//    {
//        [SetUp]
//        protected void SetUp()
//        {
            
//        }

//        //[Test]
//        //public void MainWindowCanOpenSettingsWindow()
//        //{
//        //    var mock = new Mock<IMainWindowView>();
//        //    var window = mock.Object;

//        //    mock.Setup(view => view.OnShowSettingsClicked())
//        //        .Returns(Observable.Return(EventArgs.Empty));

//        //    var settingsWindowMock = new Mock<ISettingsWindowView>();

//        //    var store = new PackageStore();
//        //    var scheduler = new TestScheduler();
//        //    var presenter = new MainWindowPresenter(window, scheduler);

//        //    presenter.System.Test(scheduler);
//        //    scheduler.Start();
//        //    scheduler.AdvanceBy(1000);
            
//        //    settingsWindowMock.Verify(v => v.Show(), Times.Once());
            
//        //    scheduler.Stop();
//        //}

//        //[Test]
//        //public void MainWindowOnlyOpensOneSettingsWindow()
//        //{
//        //    var mock = new Mock<IMainWindowView>();
//        //    var window = mock.Object;

//        //    var clickSubject = new Subject<EventArgs>();

//        //    var settingsWindowMock = new Mock<ISettingsWindowView>();

//        //    var store = new PackageStore();
//        //    var scheduler = new TestScheduler();
//        //    var presenter = new MainWindowPresenter(window, scheduler);
            
//        //    mock.Setup(view => view.OnShowSettingsClicked())
//        //        .Returns(clickSubject.AsObservable()
//        //        .ObserveOn(scheduler)
//        //        .SubscribeOn(scheduler));

//        //    presenter.System.Test(scheduler);
            
//        //    scheduler.Start();
            
//        //    clickSubject.OnNext(EventArgs.Empty);
//        //    clickSubject.OnNext(EventArgs.Empty);
//        //    clickSubject.OnNext(EventArgs.Empty);
            
//        //    scheduler.AdvanceBy(1000);
            
//        //    settingsWindowMock.Verify(v => v.Show(), Times.Exactly(3));
            
//        //    scheduler.Stop();
//        //    clickSubject.Dispose();
//        //}

//        [Test]
//        public void MainWindowDefaultsToMainPage()
//        {
//            var mock = new Mock<IMainWindowView>();

//            var store = new PackageStore(new PackageService(new MockRegistry()));
//            var scheduler = new TestScheduler();
//            var presenter = new MainWindowPresenter(mock.Object, scheduler);
//            presenter.Start();
            
//            //var x = presenter.System.Test(scheduler);
            
//            //scheduler.Start();
//            //scheduler.AdvanceBy(1000);
            
//            mock.Verify(v => v.ShowPage(It.IsAny<MainPage>()), Times.Once);
//        }

////        [Test]
////        public void MainPageAllowsSelectingPackages()
////        {
////            var mock = new Mock<IMainPageView>();
////            var store = new PackageStore();
////            var scheduler = new TestScheduler();
////            var presenter = new MainPagePresenter(mock.Object,
////                new RepositoryService(RepositoryApi.Create, Scheduler.CurrentThread),
////                store,
////                scheduler);
////
////            mock.Setup(v => v.OnPackageSelected())
////                .Returns(Observable.Return(new Package()));
////
////            presenter.System.Test(scheduler);
////            var x = store.State.Test(scheduler);
////            
////            scheduler.Start();
////            scheduler.AdvanceBy(1000);
////            
////            mock.Verify(v => v.UpdateSelectedPackages(It.IsAny<IEnumerable<Package>>()), Times.Once);
////
////            Assert.AreEqual(x.Messages.Last().Value.Value.SelectedPackages.Count, 1);
////        }

//        [Test]
//        public void MainPageShowsPackages()
//        {
            
//        }

//        [Test]
//        public void MainPageAllowsDelectingPackages()
//        {
            
//        }

//        [Test]
//        public void MainPageAllowsInstallingSelectedPackages()
//        {
            
//        }

//        [Test]
//        public void MainPageInstallationProcessChangesPageToDownloadPage()
//        {
            
//        }

//        [Test]
//        public void PackageInstallationIsCorrectlyDetected()
//        {
            
//        }

//        [Test]
//        public void PackageUpdateStatusIsCorrectlyDetected()
//        {
            
//        }

//        [Test]
//        public void VirtualDependenciesAreProperlyDetected()
//        {
            
//        }

//        [Test]
//        public void DoubleClickingTaskbarIconWillOpenMainWindow()
//        {
            
//        }

//        [Test]
//        public void SettingsWindowShouldCloseIfMainWindowClosed()
//        {
            
//        }

//        [Test]
//        public void CanWaitForProcessToCompleteWithFailure()
//        {
//            var p = new ReactiveProcess(c =>
//            {
//                c.FileName = "ls";
//                c.Arguments = "-asoidja";
//            });

//            var exitCode = 2;
            
//            var s = new TestScheduler();
//            s.Start();
            
//            var x = p.Start().Test(s);
//            s.AdvanceBy(1000);
//            p._process.WaitForExit();
            
//            Assert.AreEqual(exitCode, p._process.ExitCode);
//            Assert.AreEqual(exitCode, x.Messages[0].Value.Value);
//        }
        
//        [Test]
//        public void CanWaitForProcessToCompleteWithSuccess()
//        {
//            var p = new ReactiveProcess(c =>
//            {
//                c.FileName = "ls";
//            });

//            var exitCode = 0;
            
//            var s = new TestScheduler();
//            s.Start();
            
//            var x = p.Start().Test(s);
//            s.AdvanceBy(1000);
//            p._process.WaitForExit();
            
//            Assert.AreEqual(exitCode, p._process.ExitCode);
//            Assert.AreEqual(exitCode, x.Messages[0].Value.Value);
//        }
//    }

//    [TestFixture]
//    public class MinimumViableProduct
//    {
//        // Opens main window to package management
//        [Test]
//        public void OpenMainWindowWithMainPage()
//        {
//            var mock = new Mock<IMainWindowView>();

//            var store = new PackageStore(new PackageService(new MockRegistry()));
//            var scheduler = new TestScheduler();
//            var presenter = new MainWindowPresenter(mock.Object, scheduler);

//            //var x = presenter.System.Test(scheduler);

//            //scheduler.Start();
//            //scheduler.AdvanceBy(1000);

//            presenter.Start();
            
//            mock.Verify(v => v.ShowPage(It.IsAny<MainPage>()), Times.Once);
//        }

//        private Repository MockRepository() => MockRepository(new Uri("http://original.example"));
        
//        private Repository MockRepository(Uri uri)
//        {
//            var repoIndex = new RepoIndex(uri,
//                new Dictionary<string, string>
//                {
//                    {"en", "Test Repository"},
//                    {"sv", "Exampel Repo"}
//                },
//                new Dictionary<string, string>
//                {
//                    {"en", "Test Repository Description"},
//                    {"sv", "Exampel Repo Text"}
//                },
//                "category",
//                new List<string> { "stable" });

//            var packagesIndex = new PackagesIndex
//            {
//                Packages = new Dictionary<string, Package>()
//            };
//            var virtualsIndex = new VirtualsIndex
//            {
//                Virtuals = new Dictionary<string, List<string>>()
//            };

//            return new Repository(repoIndex, packagesIndex, virtualsIndex);
//        }
        
//        private Func<Uri, IRepositoryApi> MockRepositoryApi =>
//            uri =>
//            {
//                var mock = new Mock<IRepositoryApi>();
//                var repo = MockRepository(uri);

//                mock.Setup(x => x.RepoIndex(null))
//                    .Returns(Observable.Return(repo.Meta));

//                mock.Setup(x => x.PackagesIndex(null))
//                    .Returns(Observable.Return(repo.PackagesIndex));

//                mock.Setup(x => x.VirtualsIndex(null))
//                    .Returns(Observable.Return(repo.VirtualsIndex));

//                return mock.Object;
//            };
        
//        // Downloads the repository index
//        [Test]
//        public void DownloadRepositoryIndex()
//        {
//            var scheduler = new TestScheduler();
//            var srv = new RepositoryService(MockRepositoryApi, scheduler);

//            var t = srv.System.Test(scheduler);
//            var testUri = new Uri("https://anything.example");
            
//            scheduler.Start();
//            srv.SetRepositoryUri(testUri);
            
//            scheduler.AdvanceBy(1000);
            
//            Assert.AreEqual(testUri, t.Messages.Last().Value.Value.RepoResult.Repository.Meta.Base);
//        }

////        private IMainPageView MockMainPageView()
////        {
////            var mock = new Mock<IMainPageView>();
////            
////            mock.Setup(v => v.OnPackageSelected())
////                .Returns(Observable.Return(new BkPackage()));
////        }
        
//        // Shows the repository information
////        [Test]
////        public void MainPageUpdatesOnRepositoryIndexChange()
////        {
////            var mock = new Mock<IMainPageView>();
////            var store = new PackageStore();
////            var scheduler = new TestScheduler();
////            
////            mock.Setup(v => v.OnPackageSelected())
////                .Returns(Observable.Return(new Package()));
////            mock.Setup(v => v.OnPackageDeselected())
////                .Returns(Observable.Return(new Package()));
////            mock.Setup(v => v.OnPrimaryButtonPressed())
////                .Returns(Observable.Return(EventArgs.Empty));
////            
////            var repoServ = new RepositoryService(MockRepositoryApi, scheduler);
////            var x = repoServ.System.Test(scheduler);
////           
////            var presenter = new MainPagePresenter(mock.Object, repoServ, store, scheduler);
////
////            presenter.System.Test(scheduler);
////            store.State.Test(scheduler);
////            
////            scheduler.Start();
////            
////            var testUri = new Uri("https://lol.uri.example");
////            repoServ.SetRepositoryUri(testUri);
////            
////            scheduler.AdvanceBy(1000);
////
////            mock.Verify(v => v.UpdatePackageList(It.IsAny<Repository>()), Times.Once);
////            Assert.NotNull(x.Messages.Last().Value.Value.Repository);
////        }
        
//        // Will show the correct status if an installed package needs to be updated
//        [Test]
//        public void DemonstrateInstallationStatusHandling()
//        {
//            var reg = new MockRegistry();
//            var pkgServ = new PackageService(reg);

//            var v1 = RepositoryApi.FromJson<Package>(JsonConvert.SerializeObject(new
//            {
//                version = "3.2.0.0",
//                installer = new
//                {
//                    installedSize = 1,
//                    productCode = "test",
//                    silentArgs = "",
//                    size = 1,
//                    url = "https://lol.com"
//                }
//            }));
            
//            Assert.AreEqual(PackageInstallStatus.NotInstalled, pkgServ.InstallStatus(v1));
            
//            var subkey = reg.LocalMachine.CreateSubKey(PackageService.Keys.UninstallPath + @"\test");
            
//            subkey.Set(PackageService.Keys.DisplayVersion, "2.0.0.0", RegistryValueKind.String);
//            Assert.AreEqual(PackageInstallStatus.RequiresUpdate, pkgServ.InstallStatus(v1));
            
//            subkey.Set(PackageService.Keys.DisplayVersion, "2.99.1000.42", RegistryValueKind.String);
//            Assert.AreEqual(PackageInstallStatus.RequiresUpdate, pkgServ.InstallStatus(v1));
            
//            subkey.Set(PackageService.Keys.DisplayVersion, "3.0.0.0", RegistryValueKind.String);
//            Assert.AreEqual(PackageInstallStatus.RequiresUpdate, pkgServ.InstallStatus(v1));
            
//            subkey.Set(PackageService.Keys.DisplayVersion, "3.3.0.0", RegistryValueKind.String);
//            Assert.AreEqual(PackageInstallStatus.UpToDate, pkgServ.InstallStatus(v1));
            
//            subkey.Set(PackageService.Keys.DisplayVersion, "4.0.0.0", RegistryValueKind.String);
//            Assert.AreEqual(PackageInstallStatus.UpToDate, pkgServ.InstallStatus(v1));
            
//            subkey.Set(PackageService.Keys.DisplayVersion, "ahahahaha ahahahaha oh nø", RegistryValueKind.String);
//            Assert.AreEqual(PackageInstallStatus.ErrorParsingVersion, pkgServ.InstallStatus(v1));
//        }
        
//        // If you select packages to be installed and uninstalled, only run the installation (for now)
////        [Test]
////        public void xxx()
////        {
////            
////        }
        

//        internal class MockInstallService : IInstallService
//        {
//            private IWindowsRegistry _registry;
            
//            public MockInstallService(IWindowsRegistry registry)
//            {
//                _registry = registry;
//            }
            
////            public IObservable<ProcessResult> Install(PackageInstallInfo[] packages, Subject<OnStartPackageInfo> onStart)
////            {
////                return packages.Where(t => t.Package.Installer != null)
////                    .Select(t =>
////                    {
////                        var installer = t.Package.Installer;
////                        
////                        _registry.LocalMachine.CreateSubKey(
////                                PackageService.Keys.UninstallPath + @"\" + installer.ProductCode)
////                            .Set(PackageService.Keys.DisplayVersion,
////                                t.Package.Version,
////                                RegistryValueKind.String);
////                        
////                        return Observable.Return(new ProcessResult()
////                        {
////                            Error = "",
////                            ExitCode = 0,
////                            Output = "",
////                            Package = t.Package
////                        });
////                    }).Concat();
////            }

//            public IObservable<ProcessResult> Process(PackageProcessInfo process, Subject<OnStartPackageInfo> onStartPackage, CancellationToken token)
//            {
//                throw new NotImplementedException();
//            }
//        }

//        [Test]
//        public void DownloadProcessWorks()
//        {
            
//        }
        
//        // Upon install, go to download screen
//        [Test]
//        public void GoToDownloadScreenWithSelectedPackagesToDownload()
//        {
            
//        }
        
//        // Show things being downloaded
//        [Test]
//        public void UpdateUserInterfaceWithDownloadStatus()
//        {
            
//        }
        
//        // Upon download, go to install screen
//        [Test]
//        public void GoesToInstallScreenOnceDownloadCompleted()
//        {
            
//        }
        
//        // Install the things sequentially
//        [Test]
//        public void InstallPackagesSequentially()
//        {
            
//        }
        
//        // Return to main screen
//        [Test]
//        public void UponInstallationCompletedPrimaryFunctionReturnsToHome()
//        {
            
//        }
//    }

//    [TestFixture]
//    public class RegistryTests
//    {
//        [Test]
//        public void CanRegisterEventWatcher()
//        {
            
//        }
//    }
//}