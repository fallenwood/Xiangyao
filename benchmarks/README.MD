Benchmarks
---

Thanks to YARP, Xiangyao has a good performance against nginx static file server and aspnetcore helloworld project.

# How to run benchmark

Navigate to `<project>/benchmarks`, run

```
docker compose up
```

Open another shell, run `wrk` with headers.

```
wrk -c 125 -t 12 -d 60 -H "Host: nginx_backend:5000" http://127.0.0.1:5000/
```

Xiangyao listens on 5000, and Traefik listens on 6000

Note that Traefik accepts the host header without port, while Xiangyao needs.

# Performance Report

## 20240803

### Summary

|Load Balancer|Backend|RPS|TPS|
|-|-|-|-|
|Xiangyao|Nginx|24341.38|19.24MB|
|Traefik|Nginx|18453.81|14.59MB|
|Xiangyao|HelloWorld|33614.84|5.23MB|
|Traefik|HelloWorld|20428.28|3.18MB|

### Details

Environment
```
             .',;::::;,'.                 scrubbed
         .';:cccccccccccc:;,.             ------------------
      .;cccccccccccccccccccccc;.          OS: Fedora Linux 40 (Server Edition) x86_64
    .:cccccccccccccccccccccccccc:.        Host: 81YN (Lenovo XiaoXinAir-14ARE 2020)
  .;ccccccccccccc;.:dddl:.;ccccccc;.      Kernel: Linux 6.9.7-200.fc40.x86_64
 .:ccccccccccccc;OWMKOOXMWd;ccccccc:.     Uptime: 29 days, 9 hours, 22 mins
.:ccccccccccccc;KMMc;cc;xMMc;ccccccc:.    Packages: 820 (rpm), 499 (nix-user), 56 (nix-default)
,cccccccccccccc;MMM.;cc;;WW:;cccccccc,    Shell: bash 5.2.26
:cccccccccccccc;MMM.;cccccccccccccccc:    Terminal: /dev/pts/6
:ccccccc;oxOOOo;MMM000k.;cccccccccccc:    CPU: AMD Ryzen 5 4600U (12) @ 2.10 GHz
cccccc;0MMKxdd:;MMMkddc.;cccccccccccc;    GPU: AMD Radeon Vega Series / Radeon Vega Mobile Series [Integrated]
ccccc;XMO';cccc;MMM.;cccccccccccccccc'    Memory: 5.97 GiB / 15.24 GiB (39%)
ccccc;MMo;ccccc;MMW.;ccccccccccccccc;     Swap: 221.75 MiB / 8.00 GiB (3%)
ccccc;0MNc.ccc.xMMd;ccccccccccccccc;      Disk (/): 160.74 GiB / 464.03 GiB (35%) - xfs
cccccc;dNMWXXXWM0:;cccccccccccccc:,       Local IP (wlp2s0): 192.168.1.128/24
cccccccc;.:odl:.;cccccccccccccc:,.        Battery: 100% [AC Connected]
ccccccccccccccccccccccccccccc:'.          Locale: en_US.UTF-8
:ccccccccccccccccccccccc:;,..
 ':cccccccccccccccc::;,.

→ docker --version
Docker version 27.1.1, build 6312585

→ wrk --version
wrk 4.2.0 [epoll] Copyright (C) 2012 Will Glozer
```

<detail>

#### Xiangyao->Nginx
```
→ wrk -c 125 -t 12 -d 60 -H "Host: nginx_backend:5000" http://127.0.0.1:5000/
Running 1m test @ http://127.0.0.1:5000/
  12 threads and 125 connections
  Thread Stats   Avg      Stdev     Max   +/- Stdev
    Latency     5.07ms    3.75ms 140.42ms   95.73%
    Req/Sec     2.04k   298.98     2.43k    88.01%
  1461526 requests in 1.00m, 1.13GB read
Requests/sec:  24341.38
Transfer/sec:     19.24MB
```

#### Xiangyao->helloworld
```
→ wrk -c 125 -t 12 -d 60 -H "Host: helloworld:5000" http://127.0.0.1:5000/
Running 1m test @ http://127.0.0.1:5000/
  12 threads and 125 connections
  Thread Stats   Avg      Stdev     Max   +/- Stdev
    Latency     3.64ms    2.72ms 109.98ms   98.28%
    Req/Sec     2.82k   244.53     3.70k    92.86%
  2018818 requests in 1.00m, 313.82MB read
Requests/sec:  33614.84
Transfer/sec:      5.23MB
```

#### Traefik->Nginx

```
→ wrk -c 125 -t 12 -d 60 -H "Host: nginx_backend" http://127.0.0.1:6000/
Running 1m test @ http://127.0.0.1:6000/
  12 threads and 125 connections
  Thread Stats   Avg      Stdev     Max   +/- Stdev
    Latency     6.68ms    4.05ms  42.82ms   68.67%
    Req/Sec     1.55k   127.58     2.31k    75.54%
  1108055 requests in 1.00m, 0.86GB read
Requests/sec:  18453.81
Transfer/sec:     14.59MB
```

#### Traefik->helloworld 

```
→ wrk -c 125 -t 12 -d 60 -H "Host: helloworld" http://127.0.0.1:6000/
Running 1m test @ http://127.0.0.1:6000/
  12 threads and 125 connections
  Thread Stats   Avg      Stdev     Max   +/- Stdev
    Latency     6.02ms    3.53ms  39.20ms   70.53%
    Req/Sec     1.71k   133.28     2.68k    69.26%
  1226757 requests in 1.00m, 190.70MB read
Requests/sec:  20428.28
Transfer/sec:      3.18MB
```

</detail>