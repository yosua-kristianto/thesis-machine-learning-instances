import json;

class EnvironmentVariable:
    
    __key_value_pair = {};
   
    def __init__(self):
        self._read_env();
    
    # read_env
    # @private
    # This function will read .env file, and extracting all values as dictionary of self.key_value_pair
    def _read_env(self):
        
        try:
            with open("../config.json", "r") as fopen:
                self.__key_value_pair = json.load(fopen);
        except Exception as error:
            print(f"Failed to retrieve environment variable. Ensure that your configuration file is exist as config.json: {error}");

    # get_key
    # @public
    # This function will get a key value from key_value_pair, and return it as string.
    # If there are no value exist for the searched key, this function will throw an exception.
    def get_key(self, key):
        try:
            return self.__key_value_pair[key];
        except:
            raise Exception("Make sure the provided key is already registered in you .env!");