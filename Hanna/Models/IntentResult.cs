namespace Hanna.Models;

internal sealed record IntentResult(IntentType Type, string Query, int Limit, string RequestedDevice);
