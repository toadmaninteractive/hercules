using System.Windows;
using System.Windows.Controls;

namespace Hercules.InteractiveMaps.View
{
    /// <summary>
    /// Interaction logic for InteractiveMapView.xaml
    /// </summary>
    [ViewModelType(typeof(InteractiveMapTab))]
    public partial class InteractiveMapView : UserControl
    {
        private bool isFirstLoad = true;

        private InteractiveMapTab? Context => DataContext as InteractiveMapTab;

        public InteractiveMapView()
        {
            InitializeComponent();
        }

        private void InteractiveMapTabLoaded(object sender, RoutedEventArgs e)
        {
            if (isFirstLoad)
                isFirstLoad = false;
        }

        private void InteractiveMapTabUnloaded(object sender, RoutedEventArgs e)
        {
            Context.Editor.Properties.Value = null;
        }

        private void Test()
        {
            // X-API-Key: e8gyVkLMv7ovmNY2bH7P
            // var apiToken = "e8gyVkLMv7ovmNY2bH7P";

            // CASE 1: get database list, select database and upload image
            // var databases = PfApi.GetDatabases(apiToken);
            // var toysRUsDb = databases.FirstOrDefault(db => db.ExternalId == "toysrus");
            // var targets = PfApi.GetTargets(toysRUsDb.Id, apiToken);
            // var result = PfApi.UpsertTarget(toysRUsDb.Id, 1, 1, @"c:\Downloads\Images\Other\MarioSMBW.png", "image/png", apiToken);

            // CASE 2: create new database, get database list and delete newly created database
            // var result = PfApi.CreateDatabase("ololo", apiToken);
            // var databases = PfApi.GetDatabases(apiToken);
            // var testDb = databases.FirstOrDefault(db => db.ExternalId == "ololo");
            // result = PfApi.DeleteDatabase(testDb.Id, apiToken);

            // CASE 3: get database list, select database and upload image, then update its properties and delete it afterwards
            // var databases = PfApi.GetDatabases(apiToken);
            // var toysRUsDb = databases.FirstOrDefault(db => db.ExternalId == "toysrus");
            // var upsertResult = PfApi.UpsertTarget(toysRUsDb.Id, 1, 1, @"c:\Downloads\Images\Other\MarioSMBW.png", "image/png", apiToken);
            // var targets = PfApi.GetTargets(toysRUsDb.Id, apiToken);
            // var testTarget = targets.FirstOrDefault(target => target.ExternalId == "MarioSMBW");
            // var result = PfApi.UpdateTargetProperties(testTarget.Id, "MarioSMBW1", 22, 33, credentials);
            // result = PfApi.DeleteTarget(testTarget.Id, apiToken);

            // CASE 4: get database list, select database and download it
            // var databases = PfApi.GetDatabases(apiToken);
            // var toysRUsDb = databases.FirstOrDefault(db => db.ExternalId == "toysrus");
            // var result = PfApi.DownloadDatabase(toysRUsDb.Id, @"C:\Downloads\test_toysrus.fusionvdb", true, apiToken);
        }
    }
}
