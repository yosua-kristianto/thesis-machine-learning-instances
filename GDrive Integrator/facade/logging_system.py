from pathlib import Path
import time;
import os;

class Log:
    
    @staticmethod
    def _write_log(log, type):
        # [2023-10-01T00:00][INFO] Some message
        # Put in ../../logs
        # File name is current time session with format of [Implementation - Session Ymd H:i]
        operation = "x";
        log_path = "logs/" + time.strftime("%Y%m%d") + ".log";

        if(Path(log_path).is_file()):
            operation = "a"

        fopen = open(log_path, operation);
        
        message = "[" + time.strftime("%Y-%m-%d %H:%M:%S") + "]" + "["+ type +"] "+ log + "\n";

        fopen.write(message);
        fopen.close();
    
        if(type == "ERROR" or type == "DEBUG"):
            print(message);

    @staticmethod
    def i(log):
        Log._write_log(log, "INFO");

    @staticmethod
    def e(log):
        Log._write_log(log, "ERROR");

    @staticmethod
    def d(log):
        Log._write_log(log, "DEBUG");

    @staticmethod
    def v(log):
        Log._write_log(log, "VERBOSE");
