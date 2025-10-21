using CloudFlareDns;
using CloudFlareDns.Objects.Record;
using FlareSync;
using System.Text.Json;

string inputJson = string.Empty;
if (string.IsNullOrEmpty(args[0]))
{
    inputJson = Environment.GetEnvironmentVariable("FLARESYNC_INPUT") ?? "";
}
else
{
    inputJson = File.ReadAllText(args[0]);
}

List<DnsChange>? changes = JsonSerializer.Deserialize<List<DnsChange>>(inputJson)?.OrderBy(c => c.Order).ToList();
if(changes == null || changes.Count == 0)
{
    Console.WriteLine("No DNS changes to process.");
    return;
}

string xAuthKey = Environment.GetEnvironmentVariable("CLOUDFLARE_API_KEY") ?? "";
string xAuthEmail = Environment.GetEnvironmentVariable("CLOUDFLARE_EMAIL") ?? "";
string zoneIdentifier = Environment.GetEnvironmentVariable("CLOUDFLARE_ZONE_ID") ?? "";
CloudFlareDnsClient cloudFlareDnsClient = new (xAuthKey, xAuthEmail, zoneIdentifier);

foreach (DnsChange change in changes)
{
    Console.WriteLine($"Processing change: {JsonSerializer.Serialize(change)}");

    var records = await cloudFlareDnsClient.Record.Get();
    RecordType rt = change.Type.ToUpper() switch
    {
        "A" => RecordType.A,
        "AAAA" => RecordType.AAAA,
        "CNAME" => RecordType.CNAME,
        "TXT" => RecordType.TXT,
        "MX" => RecordType.MX,
        "SRV" => RecordType.SRV,
        "NS" => RecordType.NS,
        "PTR" => RecordType.PTR,
        "CAA" => RecordType.CAA,
        _ => throw new Exception($"Unsupported record type: {change.Type}"),
    };
    var r = records.Where(r => r.Name == change.FQDN && r.Type == rt).FirstOrDefault();

    switch (change.Action.ToLower())
    {
        case "upsert":
            if(r != null)
            {
                r.Content = change.Value;
                r.Ttl = change.TTL;
                await cloudFlareDnsClient.Record.Update(r);
            }
            else
            {
                await cloudFlareDnsClient.Record.Create(change.FQDN, change.Value, false, rt, change.TTL);
            }
            break;
        case "delete":
            if(r != null)
            {
                await cloudFlareDnsClient.Record.Delete(r.Id);
            }
            break;
        default:
            Console.WriteLine($"Unknown action: {change.Action}");
            break;
    }
}