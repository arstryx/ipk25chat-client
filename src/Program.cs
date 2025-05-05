using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ipk25_chat.ConnectionManagers;
using ipk25_chat.Messages;

namespace ipk25_chat;

class Program
{
    static async Task Main(string[] args)
    {
        
        ProtocolType protocol = ProtocolType.Unknown;
        string target_ipv4 = "";
        int port = 4567;
        int udp_timeout_ms = 250;
        int udp_retries = 3;
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h":
                    Help();
                    Environment.Exit(0);
                    break;
                
                case "-t":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine($"Not specified value for mandatory option: {args[i]}");
                        Environment.Exit(1);
                    }
                    i++;
                    if (args[i] == "udp")
                    {
                        protocol = ProtocolType.Udp;
                    }
                    else if (args[i] == "tcp")
                    {
                        protocol = ProtocolType.Tcp;
                    }
                    else
                    {
                        Console.Error.WriteLine($"Invalid value for option {args[i - 1]}: {args[i]}");
                        Environment.Exit(1);
                    }
                    break;
                
                case "-s":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine($"Not specified value for mandatory option: {args[i]}");
                        Environment.Exit(1);
                    }
                    string host = args[++i];
                    try
                    {
                        target_ipv4 = Dns.GetHostAddresses(host)
                            .First(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                            .ToString();
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Unable to resolve target {host}: {ex.Message}");
                        Environment.Exit(1);
                    }
                    break;
                
                case "-p":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine($"Not specified value for option: {args[i]}");
                        Environment.Exit(1);
                    }
                    if (!int.TryParse(args[++i], out port) || port < 1 || port > 65535)
                    {
                        Console.Error.WriteLine($"Invalid port number: {args[i]}");
                        Environment.Exit(1);
                    }
                    break;
                
                case "-d":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine($"Not specified value for option: {args[i]}");
                        Environment.Exit(1);
                    }
                    if (!int.TryParse(args[++i], out udp_timeout_ms) || udp_timeout_ms < 75 || udp_timeout_ms > 10000)
                    {
                        Console.Error.WriteLine($"Invalid timeout: {args[i]}");
                        Environment.Exit(1);
                    }
                    break;
                
                case "-r":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine($"Not specified value for option: {args[i]}");
                        Environment.Exit(1);
                    }
                    if (!int.TryParse(args[++i], out udp_retries) || udp_retries < 1 || udp_retries > 10)
                    {
                        Console.Error.WriteLine($"Invalid retries: {args[i]}");
                        Environment.Exit(1);
                    }
                    break;
                    
                
                default:
                    Console.Error.WriteLine($"Unknown argument: {args[i]}");
                    Environment.Exit(1);
                    break;         
            }
        }

        if (protocol == ProtocolType.Unknown)
        {
            Console.Error.WriteLine("Protocol not specified");
            Environment.Exit(1);
        }

        if (target_ipv4 == "")
        {
            Console.Error.WriteLine("Target IP address not specified");
            Environment.Exit(1);
        }
        
        if (protocol == ProtocolType.Tcp)
        {
            TcpConnectionManager connection_manager = new TcpConnectionManager(target_ipv4, port);
            await connection_manager.ProcessConnection();
        }
        else if (protocol == ProtocolType.Udp)
        {
            UdpConnectionManager connection_manager = new UdpConnectionManager(target_ipv4, port, udp_retries, udp_timeout_ms);
            await connection_manager.ProcessConnection();
        }
        
        Environment.Exit(0);
    }

    static void Help()
    {
        Console.WriteLine("Usage: ./ipk25_chat -s <IPv4|hostname> -p <port> -t <tcp|udp>");
        Console.WriteLine("Options:");
        Console.WriteLine("-s                             Specify the server IPv4 address or hostname.");
        Console.WriteLine("-p                             Specify the server port number (1–65535), 4567 by default.");
        Console.WriteLine("-t                             Choose the transport protocol (tcp or udp).");
        Console.WriteLine("-h                             Display this help message.");
    }
}
