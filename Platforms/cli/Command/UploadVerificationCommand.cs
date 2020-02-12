using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Heleus.Apps.Verify;
using Heleus.Base;
using Heleus.Transactions;
using Heleus.VerifyService;

namespace Heleus.Apps.Shared
{
    class UploadVerificationCommand : ServiceAccountKeyCommand
    {
        public const string CommandName = "uploadverification";
        public const string CommandDescription = "Uploads a new file verification. Returns the verification url on success.";

        string description;
        string link;
        string[] files;
        string[] filelinks;

        protected override List<KeyValuePair<string, string>> GetUsageItems()
        {
            var items = base.GetUsageItems();

            items.AddRange(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(nameof(description), "The description"),
                new KeyValuePair<string, string>(nameof(link), "A download link (optional, http[s]://)"),
                new KeyValuePair<string, string>(nameof(files), "A list of files to verify (separate with comma)"),
                new KeyValuePair<string, string>(nameof(filelinks), "A list of direkt download links for every file (optional, http[s]://)"),
            });

            return items;
        }

        protected override bool Parse(ArgumentsParser arguments)
        {
            if (!base.Parse(arguments))
                return false;

            description = arguments.String(nameof(description), null);
            link = arguments.String(nameof(link), null);
            var filesstr = arguments.String(nameof(files), null);
            if(!string.IsNullOrWhiteSpace(filesstr))
                files = filesstr.Split(',');
            var filelinksstr = arguments.String(nameof(filelinks), null);
            if(!string.IsNullOrWhiteSpace(filelinksstr))
                filelinks = filelinksstr.Split(',');

            if (!link.IsValdiUrl(true) || string.IsNullOrEmpty(description) || files == null || files.Length == 0)
                return false;

            if (filelinks != null)
            {
                if (files.Length != filelinks.Length)
                    return false;

                foreach(var fileLink in filelinks)
                {
                    if (!fileLink.IsValdiUrl(false))
                    {
                        return false;
                    }
                }
            }

            foreach (var file in files)
            {
                if (!File.Exists(file))
                {
                    SetError($"File ({file}) not found");
                    return false;
                }
            }

            return true;
        }

        string GetLink(int index)
        {
            if (filelinks == null || filelinks.Length >= index)
                return null;

            return filelinks[index];
        }

        protected override async Task Run()
        {
            var submitAccount = ServiceNode.GetSubmitAccounts<SubmitAccount>().First();

            var files = new List<VerifyFileJson>();
            for(var i = 0; i < this.files.Length; i++)
            {
                var file = this.files[i];
                try
                {
                    using(var stream = File.OpenRead(file))
                    {
                        var (hash, length) = VerifyFileJson.GenerateHash(stream);

                        var fileJson = new VerifyFileJson { name = Path.GetFileName(file), hashtype = hash.HashType.ToString().ToLower(), hash = Hex.ToString(hash.RawData), length = length, link = GetLink(i) };

                        files.Add(fileJson);
                    }
                }
                catch (Exception ex)
                {
                    Log.IgnoreException(ex);
                    return;
                }
            }

            var verifyJson = new VerifyJson { description = description, link = link, files = files };
            var result = await VerifyApp.Current.UploadVerification(submitAccount, verifyJson);
            if (result.TransactionResult == TransactionResultTypes.Ok)
                SetSuccess(VerifyApp.Current.GetRequestCode(ServiceNode, VerifyServiceInfo.ChainIndex, ViewVerificationSchemeAction.ActionName, result.Transaction.OperationId));
            else
                SetError(result.GetErrorMessage());
        }
    }
}
