import requests;
import time;
from facade.env_helper import EnvironmentVariable;

def telegram_reporter(message):
    message = "["+ time.strftime("%Y-%m-%d %H:%M:%S") +"] " + message;
    
    env = EnvironmentVariable();
    bot_id = env.get_key("TELEGRAM_BOT_ID");
    chat_id = env.get_key("CHAT_ID");
    
    print(bot_id, chat_id);

    requests.request(method = "POST", 
                     url=f"https://api.telegram.org/bot{bot_id}/sendMessage?chat_id={chat_id}&text={message}", 
                     headers={}, 
                     data={}
    );