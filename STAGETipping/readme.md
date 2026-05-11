# Goal

1. The script will read the mix well file and plate map file to calculate the tips to use for each column, and write the log at the end of the script.
2. The plate map file is prepared at the beginning of the script, and read at the end of the script to summarize the experiment.
3. The plate map file content is also written in the log file.
4. The mix well file should be prepared by the user before the experiment, and the content should be like: wellNo\tmixGroup\trow\tcolumn. The last two columns are optional, but the first two columns are required for the script to work.
5. The mix group is assigned with a color code, which will be shown in the plate map in the log file.
6. The color code is assigned based on the order of the mix group appears in the mix well file. The first 8 mix groups will be assigned with a unique color code, and the rest mix groups will be assigned with a default color code.
7. The script will also check if the mix well file is prepared correctly, and if there is any error in the mix well file, the script will write the error message in the log file and also in the plate map content in the log file.
8. The script will run at the end of the main script, so it can read all the parameters and files prepared during the experiment, and summarize the experiment in the log file.