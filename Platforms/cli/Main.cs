using System.Threading.Tasks;
using Heleus.Base;

namespace Heleus.Apps.Shared
{
    static class Program
    {
        static async Task<int> Main(string[] args)
        {
            Command.RegisterCommand<VerifyCommand>();
            Command.RegisterCommand<UploadVerificationCommand>();

            return await CLI.Run(args);
        }
    }
}
