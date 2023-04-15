<a href="#"><img src="https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/banner2-no-action.svg" /></a>

# cpu-loader

[Get it from ![Docker](https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white) hub](https://hub.docker.com/r/vladkolodka/cpu-loader)

This is a small .NET-based program designed to create load on the processor. The percentage of load and the number of virtual processors utilized can be customized. The program can generate processor load at a specific interval which can also be configured.

One example use case for this program is to generate load on an unused free-tier VM-instance to prevent it from being retired. 

Requires dotnet sdk/runtime to be installed in the system. 

### Usage
```shell
dotnet CpuLoader.dll [arguments: arg1:value arg2:value]

# Example: dotnet CpuLoader.dll procLoadPercentage:80 workMinutes:20
```
#### Arguments

| Parameter                 | Description                                                                                                                                       | Default value                   |
|---------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------|
| vProcCountToUsePercentage | Percentage of the virtual processors to use for the load generation. "1" would mean "use all", but this can cause your system to stop responding. | 0.8 (`min` - `0`, `max` - `1`)  |
| procLoadPercentage        | Determines the percentage of time the processor should be busy. In other words, the percentage of CPU utilization.                                | 40 (`min` - `0`, `max` - `100`) |
| workMinutes               | The time in minutes that the program will load the processor before the next pause.                                                               | 10 (minutes)                    |
| waitMinutes               | The pause time in minutes, after which the next iteration of load generation will begin.                                                          | 50 (minutes)                    |

### Docker

```shell
docker buildx build -f CpuLoader/Dockerfile --push --platform linux/arm64,linux/amd64 --tag vladkolodka/cpu-loader:latest .
```

#### Run
```shell
docker run -d --name cpu-loader -it --restart unless-stopped vladkolodka/cpu-loader procLoadPercentage:95
```