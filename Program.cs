// See https://aka.ms/new-console-template for more information

using S3RegexBasedDownloader.Helper;

Start:
Console.WriteLine("Enter Access Key: ");
string? accessKey = Console.ReadLine()!.Trim();

Console.WriteLine("Enter Secret Key: ");
string? secretKey = Console.ReadLine()!.Trim();

Console.WriteLine("Enter Aws Region: ");
string? region = Console.ReadLine()!.Trim();

Console.WriteLine("Enter bucket name: ");
string? bucketName = Console.ReadLine()!.Trim();

Console.WriteLine("Enter Folder Name: ");
string? folderName = Console.ReadLine()!.Trim();

Console.WriteLine("Enter Regex Pattern or Add Date Format of Date is: yyyy-mm-dd");
string? regexPattern = Console.ReadLine()!.Trim();

Console.WriteLine("Enter Search Condition yes for Date Search and No for Regex based search");
string? searchCondition = Console.ReadLine()!.Trim();

Console.WriteLine("Enter File Output Path: ");
string? filePath = Console.ReadLine()!.Trim();

if (searchCondition.ToLower().Trim() == "yes")
{
    try
    {
        var temp = DateTime.Parse(regexPattern!);
    }
    catch
    {
        Console.WriteLine("Invalid Date format");
        Console.WriteLine("Enter correct Date format of Date is: yyyy-mm-dd");
        regexPattern = Console.ReadLine()!.Trim();
    }
}



if(string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(region) || string.IsNullOrEmpty(bucketName) || string.IsNullOrEmpty(regexPattern) || string.IsNullOrEmpty(filePath)
   || string.IsNullOrEmpty(searchCondition))
{
    Console.WriteLine("Required Field : Access Key");
    Console.WriteLine("Required Field : Secret Key");
    Console.WriteLine("Required Field : Region");
    Console.WriteLine("Required Field : Bucket Name");
    Console.WriteLine("Required Field : Regex Pattern or Date");
    Console.WriteLine("Required Field : File Output Path");
    Console.WriteLine("Required Field : Search Condition");
    goto Start;
}

Console.WriteLine("Please wait while we are generating your file...");

AwsListObjectHelper listObjectHelper = new AwsListObjectHelper(accessKey!,secretKey!,region!);

MemoryStream? zipFileStream = null;

try
{
    zipFileStream = listObjectHelper.ListObjects(bucketName!, folderName, regexPattern,searchCondition).Result;
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message);
    goto Start;
}

string fileName = @$"{filePath}{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()}.zip";

using (FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
{
    zipFileStream.WriteTo(fileStream);
}

Console.WriteLine("File Generated Successfully");
