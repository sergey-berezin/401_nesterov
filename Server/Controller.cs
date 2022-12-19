using Microsoft.AspNetCore.Mvc;

using Contracts;


namespace Server
{
    [Route("api/faceEmbeddings")]
    [ApiController]
    public class Controller : ControllerBase
    {
        private readonly ImageEmbeddingProcessor context;
        public Controller(ImageEmbeddingProcessor context)
        {
            this.context = context;
        }

        [HttpPost]
        [Route("images")]
        public async Task<List<int>> PostImages([FromBody] List<ImageDetails> images, CancellationToken token)
        {
            return await context.ProcessImagesAsync(images, token);
        }

        [HttpGet]
        [Route("images")]
        public async Task<ActionResult<List<ImageDetails>>> GetImages()
        {
            var res = await context.GetImages();
            if (res != null)
                return res;
            else
                return StatusCode(404, "Can't return images.");
        }

        [HttpGet]
        [Route("compare")]
        public async Task<ActionResult<Dictionary<string, object>>>
            Compare([FromQuery] int id1, [FromQuery] int id2)
        {
            try
            {
                var res = context.Compare(new Tuple<int, int>(id1, id2));
                if (res != null)
                    return res;
                else
                    return StatusCode(404, "No such images in database.");
            }
            catch (Exception ex)
            {
                return StatusCode(400, ex.Message);
            }
        }

        [HttpDelete]
        [Route("images/id")]
        public async Task<ActionResult<bool>> DeleteImage(int id)
        {
            var success = await context.DeleteImageById(id);
            if (success)
                return success;
            return StatusCode(404, $"No image with id {id} in databse");
        }

        [HttpDelete]
        [Route("images")]
        public async Task<ActionResult<bool>> Clear()
        {
            var success = await context.Clear();
            if (success)
                return success;
            return StatusCode(400, "Database Clear failed.");
        }
    }
}
