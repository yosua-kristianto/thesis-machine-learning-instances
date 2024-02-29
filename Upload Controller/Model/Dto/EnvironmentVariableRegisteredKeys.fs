namespace Model.Dto

type RegisteredKeys = {
    TELEGRAM_BOT_ID: string
    CHAT_ID: int64
    
    ORIGINAL_IMAGE_DIRECTORY: string
    DOWNSCALED_UPSCALED_IMAGE_FOLDER_PATH: string
    OCR_GROUND_TRUTH_PATH: string

    YOLO_ANNOTATION_FOLDER_OUTPUT: string
    YOLO_IMAGE_FOLDER: string

    DATABASE_CONNECTION: string
};
