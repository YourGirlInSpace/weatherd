# weatherd

weatherd was created to be a dual platform data handling service that reads information from a Campbell Scientific data logger and writes the same data to AWS Timestream.  Due to the lack of Linux support and the proprietary data protocol used by Campbell Scientific data loggers, a significant amount of reverse-engineering was needed to be done to support the serial Pakbus data protocol to communicate with the data logger.

This service interacts with a Campbell Scientific data logger using Pakbus on a serial interface.  By default, the service will assume `/dev/ttyUSB0`.  Feel free to customize this service to your individual needs.
