namespace LocalMonitor.Models;

public class HostUp
{
    public string? Ip { get; set; }
    public string? MAC { get; set; }
    public string? Name { get; set; }
}

public class IpList
{
    private List<HostUp> ipList;

    public IpList()
    {
        ipList = new List<HostUp>();
    }

    public void AppendToList(string ip, string mac, string name)
    {
        ipList.Add(new HostUp{Ip = ip, MAC = mac, Name = name});
    }

    public List<HostUp> GetList()
    {
        return ipList;
    }
}