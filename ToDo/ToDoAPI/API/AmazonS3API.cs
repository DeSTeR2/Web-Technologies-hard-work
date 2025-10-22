using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ToDoAPI.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class AmazonS3Controller : ControllerBase
    {
        private readonly IAmazonS3 _s3;

        public AmazonS3Controller(IAmazonS3 s3) => _s3 = s3;

        [HttpGet("buckets")]
        public async Task<IActionResult> GetAllBuckets()
        {
            var response = await _s3.ListBucketsAsync();
            var names = response.Buckets.Select(b => b.BucketName);
            return Ok(names);
        }

        [HttpPost("buckets/{bucketName}")]
        public async Task<IActionResult> CreateBucket(string bucketName)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
                return BadRequest("bucketName is required.");

            if (bucketName.Length is < 3 or > 63)
                return BadRequest("Bucket name must be 3-63 characters.");
            
            bool exists;
            try
            {
                var loc = await _s3.GetBucketLocationAsync(new GetBucketLocationRequest
                {
                    BucketName = bucketName
                });
                exists = true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                exists = false;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Conflict("Bucket exists but is not accessible with current credentials.");
            }

            if (exists)
                return Conflict("Bucket already exists.");

            var putResp = await _s3.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName
            });

            return putResp.HttpStatusCode == System.Net.HttpStatusCode.OK
                ? CreatedAtAction(nameof(GetAllBuckets), new { bucketName }, bucketName)
                : StatusCode((int)putResp.HttpStatusCode);
        }


        [HttpDelete("buckets/{bucketName}")]
        public async Task<IActionResult> DeleteBucket(string bucketName)
        {
            await _s3.DeleteBucketAsync(new DeleteBucketRequest { BucketName = bucketName });
            return Ok($"Bucket '{bucketName}' deleted.");
        }

        [HttpGet("{bucketName}/files")]
        public async Task<IActionResult> ListFiles(string bucketName)
        {
            var response = await _s3.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucketName
            });

            var files = response.S3Objects.Select(o => new
            {
                o.Key,
                o.Size,
                o.LastModified
            });

            return Ok(files);
        }

        [HttpPost("{bucketName}/upload")]
        public async Task<IActionResult> UploadFile(string bucketName, IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty.");

            await using var stream = file.OpenReadStream();
            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = file.FileName,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3.PutObjectAsync(request);
            return Ok($"Uploaded '{file.FileName}' to bucket '{bucketName}'.");
        }

        [HttpGet("{bucketName}/download/{key}")]
        public async Task<IActionResult> DownloadFile(string bucketName, string key)
        {
            var response = await _s3.GetObjectAsync(bucketName, key);
            return File(response.ResponseStream, response.Headers.ContentType ?? "application/octet-stream", key);
        }

        [HttpDelete("{bucketName}/files/{key}")]
        public async Task<IActionResult> DeleteFile(string bucketName, string key)
        {
            await _s3.DeleteObjectAsync(bucketName, key);
            return Ok($"File '{key}' deleted from bucket '{bucketName}'.");
        }
    }
}
