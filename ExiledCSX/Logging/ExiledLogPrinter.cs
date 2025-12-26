using ExiledCSX.Extensions;
using Mono.CSharp;
namespace ExiledCSX.Logging
{
    public class ExiledCsxPrinter : ReportPrinter
    {
        public override void Print(AbstractMessage msg, bool showContext)
        {
            if (msg.IsWarning) return;
            Log.ErrorScript($"{msg.MessageType} {msg.Code}: {msg.Text} at location: {msg.Location}");
        }
    }
}