using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Heleus.Base;
using Heleus.VerifyService;

namespace Heleus.Apps.Shared
{

    class VerifyCommand : CustomEndpointCommand
    {
        public const string CommandName = "verify";
        public const string CommandDescription = "Verifies a file. Returns the hash on success.";

        long id;
        string file;

        protected override List<KeyValuePair<string, string>> GetUsageItems()
        {
            var items = base.GetUsageItems();

            items.AddRange(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(nameof(id), "The verification id"),
                new KeyValuePair<string, string>(nameof(file), "The file to verify")
            });

            return items;
        }

        protected override bool Parse(ArgumentsParser arguments)
        {
            if (!base.Parse(arguments))
                return false;

            id = arguments.Long(nameof(id), -1);
            file = arguments.String(nameof(file), null);

            if (string.IsNullOrEmpty(file) || id < 0)
                return false;

            if (!File.Exists(file))
            {
                SetError($"File ({file}) not found");
                return false;
            }

            return true;
        }

        protected override async Task Run()
        {
            (var hash, var length, _) = VerifyFileJson.GenerateHash(file);
            var verify = await VerifyApp.Current.DownloadVerificationResult(ServiceNode, id);
            if (verify != null)
            {
                try
                {
                    foreach (var f in verify.Verify.files)
                    {
                        if (f.length == length && f.GetHash() == hash)
                        {
                            SetSuccess(f.hash);
                            return;
                        }
                    }
                }
                catch(Exception ex)
                {
                    Log.HandleException(ex);
                }

                SetError("File is not valid");
            }
            else
            {
                SetError($"Verification {id} not found");
            }
        }
    }
}
