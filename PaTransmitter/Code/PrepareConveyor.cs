using PaTransmitter.Code.Services;
using PaTransmitter.Code.Transport;

namespace PaTransmitter.Code
{
    public class PrepareConveyor
    {
        private WebApplication app;

        public PrepareConveyor(WebApplication app)
        {
            this.app = app;
        }

        public async Task Prepare()
        {
            try
            {
                app.Logger.LogInformation("Initializing file and transport managers..");
                // Initializing file and transport managers singletons.
                var fileManager = FileManager.CreateInstance();
                fileManager.Setup(app);

                var transportManager = TransportsManager.CreateInstance();
                await transportManager.InitializeTransports(app);
            }
            catch (Exception ex)
            {
                app.Logger.LogInformation(ex.Message);
            }
        }
    }
}
