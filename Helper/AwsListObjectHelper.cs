using System.IO.Compression;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace S3RegexBasedDownloader.Helper;

public class AwsListObjectHelper
{
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly RegionEndpoint _regionEndpoint;

    public AwsListObjectHelper(string accessKey, string secretKey, string regionName)
    {
        _accessKey = accessKey;
        _secretKey = secretKey;
        _regionEndpoint = RegionEndpoint.GetBySystemName(regionName);
    }
    public async Task<MemoryStream> ListObjects(string bucketName, string? prefix,string? regexPattern,string searchCondition)
    {
        Dictionary<string,MemoryStream> allRequestedFiles = new Dictionary<string, MemoryStream>();
        using var client = new AmazonS3Client(_accessKey, _secretKey, _regionEndpoint);
        ListObjectsV2Request  listRequest = string.IsNullOrEmpty(prefix)
            ? new ListObjectsV2Request 
            {
                BucketName = bucketName,
            }
            : new ListObjectsV2Request ()
            {
                BucketName = bucketName,
                Prefix = prefix,
                StartAfter = prefix,
            };

        ListObjectsV2Response  listResponse;
            
        do
        {
            listResponse = client.ListObjectsV2Async(listRequest).GetAwaiter().GetResult();
            foreach (S3Object obj in listResponse.S3Objects)
            {
                string fileName = Path.GetFileName(obj.Key);

                if (searchCondition.ToLower() == "yes")
                {
                    var dateData = DateTime.Parse(regexPattern!);
                    if (obj.LastModified.Date >= dateData.Date)
                    {
                        // Download the object
                        var memoryStream = await DownloadObjectAsync(client, bucketName, obj.Key);
                        allRequestedFiles[fileName] = memoryStream;
                    }
                }
                else
                {
                    if (Regex.IsMatch(fileName, regexPattern!))
                    {
                        // Download the object
                        var memoryStream = await DownloadObjectAsync(client, bucketName, obj.Key);
                        allRequestedFiles[fileName] = memoryStream;
                    }
                }
            }
                
            listRequest.ContinuationToken = listResponse.NextContinuationToken;
        } while (listResponse.IsTruncated);

        return CreateZipFile(allRequestedFiles);
    }
    private async Task<MemoryStream> DownloadObjectAsync(AmazonS3Client s3Client, string bucketName, string objectKey)
    {
        TransferUtility fileTransferUtility = new TransferUtility(s3Client,new TransferUtilityConfig()
        {
            ConcurrentServiceRequests = 10,
        });
        Stream stream = await fileTransferUtility.OpenStreamAsync(bucketName, objectKey);
        MemoryStream memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }
    private MemoryStream CreateZipFile(Dictionary<string, MemoryStream> nestedFileNames)
    {
        MemoryStream compressedFileStream = new MemoryStream();

        using var zipArchive = new ZipArchive(compressedFileStream, ZipArchiveMode.Create, true);

        foreach (var item in nestedFileNames)
        {
            ZipArchiveEntry zipEntry = zipArchive.CreateEntry(item.Key);

            using var zipEntryStream = zipEntry.Open();
            item.Value.Seek(0, SeekOrigin.Begin);
            item.Value.CopyTo(zipEntryStream);
        }
        return compressedFileStream;
    }
}