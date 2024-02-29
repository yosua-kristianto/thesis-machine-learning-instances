# Standardizing the running script from the root of working directory
conda run -n google-drive-sdk python '.\GDrive Integrator\main.py'

python '.\Preprocessing\py\transform.py'  --data_path="the-bin-file-contained-folder" --new_data_path="target-folder"
