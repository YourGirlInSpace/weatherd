[![Linux](https://github.com/YourGirlInSpace/weatherd/actions/workflows/dotnet-linux.yml/badge.svg)](https://github.com/YourGirlInSpace/weatherd/actions/workflows/dotnet-linux.yml) [![Windows](https://github.com/YourGirlInSpace/weatherd/actions/workflows/dotnet.yml/badge.svg)](https://github.com/YourGirlInSpace/weatherd/actions/workflows/dotnet.yml)

# weatherd

weatherd is a dual platform data handling service that interfaces with a Campbell Scientific data logger or a Vaisala PWD-style transmissometer, processes the data and writes it to AWS Timestream.  Due to the lack of Linux support and the proprietary data protocol used by Campbell Scientific data loggers, a significant amount of reverse-engineering was needed to be done to support the serial Pakbus data protocol to communicate with the data logger.

This service interacts with its sensors using an RS232 interface.  By default, the service will map `/dev/ttyUSB0` to the Campbell Scientific data source and `/dev/ttyUSB1` to the Vaisala PWD12 data source.  Feel free to customize this service to your individual needs.

This service transmits the following data to Timestream:
- Time
- Temperature
- Dewpoint
- Relative Humidity
- Pressure
- Sea Level Pressure
- Luminosity
- Wind Speed
- Wind Direction
- Rainfall since midnight station time
- Snowfall since midnight station time
- Visibility
- Weather code
