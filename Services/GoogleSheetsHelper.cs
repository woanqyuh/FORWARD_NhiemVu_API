using ForwardMessage.Dtos;
using ForwardMessage.Repositories;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Quartz;

namespace ForwardMessage.Services
{
    public class GoogleSheetsHelper
    {
        private readonly SheetsService _sheetsService;
        private readonly string spreadsheetId;
        private readonly string range;

        public GoogleSheetsHelper(IConfiguration config, IHostEnvironment environment)
        {

            var rootDirectory = environment.ContentRootPath;
            var credentialsPath = Path.Combine(rootDirectory, "credentials.json");
            using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
            var credential = GoogleCredential.FromStream(stream).CreateScoped(SheetsService.Scope.Spreadsheets);

            _sheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Sheet API",
            });

            spreadsheetId = config["GoogleSheets:SpreadsheetId"] ?? string.Empty;
            range =  config["GoogleSheets:Range"] ?? string.Empty;
        }

        public async Task<IList<IList<object>>> ReadSheetAsync()
        {
            var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = await request.ExecuteAsync();
            return response.Values;
        }
    }
    public class SyncGoogleSheetsJob : IJob
    {
        private readonly GoogleSheetsHelper _sheetsHelper;
        private readonly ISheetRepository _sheetRepository;

        public SyncGoogleSheetsJob(GoogleSheetsHelper sheetsHelper, ISheetRepository sheetRepository)
        {
            _sheetsHelper = sheetsHelper;
            _sheetRepository = sheetRepository;
        }

        public async Task Execute(IJobExecutionContext context)
        {

            Console.WriteLine("SyncGoogleSheetsJob Starting");
            var data = await _sheetsHelper.ReadSheetAsync();
            var newData = new List<SheetDto>();
            var usersToSave = new List<(string username, string teleId)>();

            foreach (var row in data)
            {
                if (row.Count > 2)
                {
                    var name = row[0].ToString();
                    var username = row[1].ToString();
                    var teleId = row[2].ToString();

                    newData.Add(new SheetDto
                    {
                        Username = username,
                        ChatId = teleId,
                    });
                }
            }
            await _sheetRepository.SyncSheetAsync(newData);
            Console.WriteLine("SyncGoogleSheetsJob: Finished.Repeat after 30minute");
        }
    }
}
