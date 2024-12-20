using Hercules.Documents;
using System;

namespace Hercules
{
    public static class UrlDispatcher
    {
        public static bool TryDispatch()
        {
            var dispatchArg = Core.GetCliArgument("-dispatch");
            if (dispatchArg == null || !dispatchArg.StartsWith("hercules:"))
                return false;
            if (!Uri.TryCreate(dispatchArg, UriKind.Absolute, out var dispatchUri))
                return false;
            if (!HerculesUrl.TryGetDatabaseHerculesUrl(dispatchUri, out var dbUrl))
                return false;
            var pipeName = $"Global\\{dbUrl}";
            if (System.IO.File.Exists($@"\\.\pipe\{pipeName}"))
            {
                try
                {
                    NamedPipeServer.SendMessage(pipeName, dispatchArg);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    return false;
                }
            }
            return false;
        }

        public static void SendPipeMessage(string pipename, string message)
        {

        }
    }
}
