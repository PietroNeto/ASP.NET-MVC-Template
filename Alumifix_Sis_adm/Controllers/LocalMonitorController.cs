using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LocalMonitor.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Collections.Generic;

namespace LocalMonitor.Controllers;

public class LocalMonitorController : Controller
{
    private readonly ILogger<LocalMonitorController> _logger;

    public LocalMonitorController(ILogger<LocalMonitorController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [ActionName("IpList")]
    public IActionResult GetIpList([FromServices] ILogger<LocalMonitorController> logger, string id)
    {
        List<string> ips = new List<string>();

        if(id != null)
        {
            string[] ip_data = id.Split("+");
            string rootIP = ip_data[0];
            int maskBits = 32;
            if (ip_data.Length >=2 ){
                maskBits = int.Parse(ip_data[1]);
            }

            if (maskBits != 32)
            {
                // Parse the root IP into its octets
                string[] octets = rootIP.Split('.');
                if (octets.Length != 4)
                {
                    return new JsonResult("Root IP must consist of 4 octets separated by periods.");
                }

                // Convert the mask bits into a subnet mask
                int mask = (int)Math.Pow(2, maskBits) - 1;
                mask <<= (32 - maskBits);

                // Generate all possible IPs within the subnet
                int baseIP = 0;
                for (int i = 0; i < 4; i++)
                {
                    baseIP |= int.Parse(octets[i]) << (8 * (3 - i));
                }
                int netIP = baseIP & mask;
                int numHosts = (int)Math.Pow(2, 32 - maskBits) - 2;
                for (int i = 0; i < numHosts; i++)
                {
                    int hostIP = netIP | i + 1;
                    string humanReadableIP = $"{(hostIP >> 24) & 0xFF}.{(hostIP >> 16) & 0xFF}.{(hostIP >> 8) & 0xFF}.{hostIP & 0xFF}";
                    ips.Add(humanReadableIP);
                }
            }
            else
            {
                ips.Add(rootIP);
            }
        } 
        else
        {
            return new JsonResult("No Scan Parameters Informed.");
        }

        // Ping IPs and Scan the HostUp
        IpList iplist = new IpList();
        foreach (string ip in ips)
        {
            Ping ping = new Ping();
            PingReply reply = ping.Send(ip, 100);
            
            if (reply.Status == IPStatus.Success)
            {
                
                string HostName="<unknown>";
                string macAddress="<unknown>";
                string[] resp;
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo("nbtscan", "-s / " + ip);
                    startInfo.RedirectStandardOutput = true;
                    startInfo.UseShellExecute = false;

                    using (Process process = Process.Start(startInfo))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        resp = output.Split("/");
                    }
                    if(resp.Length >= 5)
                    {
                        HostName = resp[1].Replace(" ","");
                        macAddress = resp[4].Replace("\n","");
                        iplist.AppendToList(ip,macAddress,HostName);
                    } 
                    else 
                    {
                        ProcessStartInfo startInfo2 = new ProcessStartInfo("arping", "-c 1 -w 1 " + ip);
                        startInfo2.RedirectStandardOutput = true;
                        startInfo2.UseShellExecute = false;

                        using (Process process = Process.Start(startInfo2))
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            int index = output.IndexOf("from");
                            if (index >= 0)
                            {
                                macAddress = output.Substring(index + 5, 17);
                                iplist.AppendToList(ip,macAddress,HostName);
                            }
                        }
                    }
                    
                }
                catch
                {
                    iplist.AppendToList(ip,"Fail","Fail");
                }
                
            }
        }

        return new JsonResult(iplist.GetList());
    }
}