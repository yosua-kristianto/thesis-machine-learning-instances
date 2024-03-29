from calendar import EPOCH
import os.path;
import mimetypes;
import json;
from datetime import datetime, timezone;

from google.auth.transport.requests import Request
from google.oauth2.credentials import Credentials
from google_auth_oauthlib.flow import InstalledAppFlow
from googleapiclient.discovery import build
from googleapiclient.errors import HttpError
from googleapiclient.http import MediaFileUpload
from google.auth.transport.requests import Request

from facade.logging_system import Log;
from facade.telegram_service import telegram_reporter;
from facade.env_helper import EnvironmentVariable;


class GoogleDriveService:
    
    # If modifying these scopes, delete the file token.json.
    SCOPES = []; 
    credential = None;

    def __init__(self):
      env = EnvironmentVariable();
      self.SCOPES = env.get_key("GDRIVE_API_SCOPES");

      # The file token.json stores the user's access and refresh tokens, and is
      # created automatically when the authorization flow completes for the first
      # time.
      if os.path.exists("token.json"):
        self.credential = Credentials.from_authorized_user_file("token.json", self.SCOPES)
        
      # If there are no (valid) credentials available, let the user log in.
      if not self.credential or not self.credential.valid:
        if self.credential and self.credential.expired and self.credential.refresh_token:
          self.credential.refresh(Request())
        else:
          flow = InstalledAppFlow.from_client_secrets_file(
              "credentials.json", self.SCOPES
          )
          self.credential = flow.run_local_server(port=0)
        # Save the credentials for the next run
        with open("token.json", "w") as token:
          token.write(self.credential.to_json())

    def check_required_refresh_token(self):
       if (os.path.exists("token.json")):
            epoch_now = datetime.now().timestamp();

            with open("token.json", "r") as fopen:
                data = json.loads(fopen.read());
            token_expirity = data["expiry"];

            epoch_expirity = datetime.fromisoformat(token_expirity).replace(tzinfo=timezone.utc);
            # print(epoch_expirity, datetime.utcnow());
            epoch_expirity = epoch_expirity.timestamp();
            
            # print(epoch_expirity, epoch_now);
            
            # print(str(epoch_expirity - epoch_now))
            
            if((epoch_expirity - epoch_now) < 3600):
                Log.d("Refresh token conducted.");
                self.credential.refresh(Request());
                
                # Re-write token
                with open("token.json", "w") as token:
                    token.write(self.credential.to_json())
                
       else:
          Log.d("Authentication has not been recorded. Checking refresh token is unnessecary");

    def service(self):
        return build("drive", "v3", credentials=self.credential)
        
    def upload_file(self, path_origin: str, folder_id: str):
        file_name: str = os.path.basename(path_origin);
        file_mimetype: str = mimetypes.guess_type(path_origin)[0];
        
        try:
            file_metadata = {"name": file_name, "parents": [folder_id]};
            media = MediaFileUpload(path_origin, mimetype = file_mimetype);

            upload = (
                self.service().files().create(body = file_metadata, media_body = media, fields = "id").execute()
            );
            
            Log.d(f"Successfully uploaded {file_name} with type of {file_mimetype}");
        except Exception as error:
            Log.e(f"Upload error with: {error}");
            telegram_reporter(f"Upload error with message of: {error}")
            