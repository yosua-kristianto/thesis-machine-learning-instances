from services.google_drive_services import GoogleDriveService;
import pandas;
import sys;
from facade.logging_system import Log;

if __name__ == "__main__":
   
   # Argument extractors

   file_origin_path = "";
   folder_code = "";

   for i in sys.argv:
       if(i.__contains__(".py")):
           continue;
       
       if(not i.__contains__("=") or not i.startswith("--")):
           raise Exception("Every arguments must consist --[config_name]=[value] format\n" + "Supported arguments:\n" + "--file_origin_path : This mark the origin file path to be uploaded\n" + "--folder_code      : This mark the target folder the file will be uploaded at\n" + "e.g. --file_origin_path=absolute/path/to/file.ext");
       
       argument = i.replace("--", "");
       argument = argument.split("=");
        
       if(argument[0] == "file_origin_path"):
           file_origin_path = argument[1];
       elif(argument[0] == "folder_code"):
           folder_code = argument[1];
    
   # Get folder id from csv
   dataframe = pandas.read_csv("data.csv");

   repo = dataframe.where((dataframe["FOLDER_CODE"] == folder_code));
   repo = repo.dropna();
   repo = repo.reset_index();
   folder_id = repo["FOLDER_ID"][0];

   # Call API
   service = GoogleDriveService();
   service.check_required_refresh_token();
   service.upload_file(file_origin_path, folder_id);

        
   

