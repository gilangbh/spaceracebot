using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyStupidBots
{
    class FBPost
    {
        public string id { get; set; }
    }
    class Facebook
    {
        readonly string _accessToken;
        readonly string _pageID;
        readonly string _facebookAPI = "https://graph.facebook.com/";
        readonly string _facebookVideoAPI = "https://graph-video.facebook.com/";
        readonly string _pageEdgeFeed = "feed";
        readonly string _pageEdgePhotos = "photos";
        readonly string _pageEdgeVideos = "videos";
        readonly string _postToPageURL;
        readonly string _postToPagePhotosURL;
        readonly string _postToPageVideosURL;

        public Facebook(string accessToken, string pageID)
        {
            _accessToken = accessToken;
            _pageID = pageID;
            _postToPageURL = $"{_facebookAPI}{pageID}/{_pageEdgeFeed}";
            _postToPagePhotosURL = $"{_facebookAPI}{pageID}/{_pageEdgePhotos}";
            _postToPageVideosURL = $"{_facebookVideoAPI}{pageID}/{_pageEdgeVideos}";
        }

        /// <summary>
        /// Publish a simple text post
        /// </summary>
        /// <returns>StatusCode and JSON response</returns>
        /// <param name="postText">Text for posting</param>
        public async Task<Tuple<int, string>> PublishSimplePost(string postText)
        {
            using (var http = new HttpClient())
            {
                var postData = new Dictionary<string, string> {
                    { "access_token", _accessToken },
                    { "message", postText }//,
                    // { "formatting", "MARKDOWN" } // doesn't work
                };

                var httpResponse = await http.PostAsync(
                    _postToPageURL,
                    new FormUrlEncodedContent(postData)
                    );
                var httpContent = await httpResponse.Content.ReadAsStringAsync();

                return new Tuple<int, string>(
                    (int)httpResponse.StatusCode,
                    httpContent
                    );
            }
        }

        /// <summary>
        /// Publish a simple text post
        /// </summary>
        /// <returns>StatusCode and JSON response</returns>
        /// <param name="postText">Text for posting</param>
        public async Task<Tuple<int, string>> PublishSimplePostAndGetFBPostID(string postText)
        {
            using (var http = new HttpClient())
            {
                var postData = new Dictionary<string, string> {
                    { "access_token", _accessToken },
                    { "message", postText }//,
                    // { "formatting", "MARKDOWN" } // doesn't work
                };

                var httpResponse = await http.PostAsync(
                    _postToPageURL,
                    new FormUrlEncodedContent(postData)
                    );
                var httpContent = await httpResponse.Content.ReadAsStringAsync();
                FBPost post= JsonConvert.DeserializeObject<FBPost>(httpContent);

                return new Tuple<int, string>(
                    (int)httpResponse.StatusCode,
                    post.id
                    );
            }
        }
        /// <summary>
        /// Publish a post to Facebook page
        /// </summary>
        /// <returns>Result</returns>
        /// <param name="postText">Post to publish</param>
        /// <param name="pictureURL">Post to publish</param>
        public string PublishToFacebook(string postText, string pictureURL)
        {
            try
            {
                // upload picture first
                var rezImage = Task.Run(async () =>
                {
                    using (var http = new HttpClient())
                    {
                        return await UploadPhoto(pictureURL);
                    }
                });
                var rezImageJson = JObject.Parse(rezImage.Result.Item2);

                if (rezImage.Result.Item1 != 200)
                {
                    try // return error from JSON
                    {
                        return $"Error uploading photo to Facebook for {pictureURL}. {rezImageJson["error"]["message"].Value<string>()}";
                    }
                    catch (Exception ex) // return unknown error
                    {
                        // log exception somewhere
                        return $"Unknown error uploading photo to Facebook. {ex.Message}";
                    }
                }
                // get post ID from the response
                string postID = rezImageJson["post_id"].Value<string>();

                // and update this post (which is actually a photo) with your text
                var rezText = Task.Run(async () =>
                {
                    using (var http = new HttpClient())
                    {
                        return await UpdatePhotoWithPost(postID, postText);
                    }
                });
                var rezTextJson = JObject.Parse(rezText.Result.Item2);

                if (rezText.Result.Item1 != 200)
                {
                    try // return error from JSON
                    {
                        return $"Error posting to Facebook. {rezTextJson["error"]["message"].Value<string>()}";
                    }
                    catch (Exception ex) // return unknown error
                    {
                        // log exception somewhere
                        return $"Unknown error posting to Facebook. {ex.Message}";
                    }
                }

                return postID;
            }
            catch (Exception ex)
            {
                // log exception somewhere
                return $"Unknown error publishing post to Facebook. {ex.Message}";
            }
        }

        /// <summary>
        /// Upload a picture (photo)
        /// </summary>
        /// <returns>StatusCode and JSON response</returns>
        /// <param name="photoURL">URL of the picture to upload</param>
        public async Task<Tuple<int, string>> UploadPhoto(string photoURL)
        {
            using (var http = new HttpClient())
            {
                var postData = new Dictionary<string, string> {
                    { "access_token", _accessToken },
                    { "url", photoURL }
                };

                var httpResponse = await http.PostAsync(
                    _postToPagePhotosURL,
                    new FormUrlEncodedContent(postData)
                    );
                var httpContent = await httpResponse.Content.ReadAsStringAsync();

                return new Tuple<int, string>(
                    (int)httpResponse.StatusCode,
                    httpContent
                    );
            }
        }


        /// <summary>
        /// Publish a post to Facebook page
        /// </summary>
        /// <returns>Result</returns>
        /// <param name="postText">Post to publish</param>
        /// <param name="pictureURL">Post to publish</param>
        public string PublishScheduledToFacebook(string postText, string pictureURL, int publishtimestamp)
        {
            try
            {
                // upload picture first
                var rezImage = Task.Run(async () =>
                {
                    using (var http = new HttpClient())
                    {
                        return await UploadPhotoScheduledPost(pictureURL, publishtimestamp, postText);
                    }
                });
                var rezImageJson = JObject.Parse(rezImage.Result.Item2);

                if (rezImage.Result.Item1 != 200)
                {
                    try // return error from JSON
                    {
                        return $"Error uploading photo to Facebook for {pictureURL}. {rezImageJson["error"]["message"].Value<string>()}";
                    }
                    catch (Exception ex) // return unknown error
                    {
                        // log exception somewhere
                        return $"Unknown error uploading photo to Facebook. {ex.Message}";
                    }
                }
                // get post ID from the response
                string postID = rezImageJson["id"].Value<string>();

                // // and update this post (which is actually a photo) with your text
                // var rezText = Task.Run(async () =>
                // {
                //     using (var http = new HttpClient())
                //     {
                //         return await UpdatePhotoWithPost(postID, postText);
                //     }
                // });
                // var rezTextJson = JObject.Parse(rezText.Result.Item2);

                // if (rezText.Result.Item1 != 200)
                // {
                //     try // return error from JSON
                //     {
                //         return $"Error posting to Facebook. {rezTextJson["error"]["message"].Value<string>()}";
                //     }
                //     catch (Exception ex) // return unknown error
                //     {
                //         // log exception somewhere
                //         return $"Unknown error posting to Facebook. {ex.Message}";
                //     }
                // }

                return postID;
            }
            catch (Exception ex)
            {
                // log exception somewhere
                return $"Unknown error publishing post to Facebook. {ex.Message}";
            }
        }

        /// <summary>
        /// Upload a picture (photo) and schedule the post
        /// </summary>
        /// <returns>StatusCode and JSON response</returns>
        /// <param name="photoURL">URL of the picture to upload</param>
        public async Task<Tuple<int, string>> UploadPhotoScheduledPost(string photoURL, int publishtimestamp, string message)
        {
            using (var http = new HttpClient())
            {
                var postData = new Dictionary<string, string> {
                    { "access_token", _accessToken },
                    { "url", photoURL },
                    { "scheduled_publish_time", publishtimestamp.ToString()},
                    { "published", "false"},
                    { "message", message }
                };

                var httpResponse = await http.PostAsync(
                    _postToPagePhotosURL,
                    new FormUrlEncodedContent(postData)
                    );
                var httpContent = await httpResponse.Content.ReadAsStringAsync();

                return new Tuple<int, string>(
                    (int)httpResponse.StatusCode,
                    httpContent
                    );
            }
        }
        public async Task<Tuple<int, string>> UploadVideo(string videoUrl, string description)
        {
            using (var http = new HttpClient())
            {
                var postData = new Dictionary<string, string> {
                { "access_token", _accessToken },
                { "file_url", videoUrl },
                { "description", description}
            };

                var httpResponse = await http.PostAsync(
                    _postToPageVideosURL,
                    new FormUrlEncodedContent(postData)
                    );
                var httpContent = await httpResponse.Content.ReadAsStringAsync();

                return new Tuple<int, string>(
                    (int)httpResponse.StatusCode,
                    httpContent
                    );
            }
        }

        /// <summary>
        /// Update the uploaded picture (photo) with the given text
        /// </summary>
        /// <returns>StatusCode and JSON response</returns>
        /// <param name="postID">Post ID</param>
        /// <param name="postText">Text to add tp the post</param>
        public async Task<Tuple<int, string>> UpdatePhotoWithPost(string postID, string postText)
        {
            using (var http = new HttpClient())
            {
                var postData = new Dictionary<string, string> {
                    { "access_token", _accessToken },
                    { "message", postText }//,
                    // { "formatting", "MARKDOWN" } // doesn't work
                };

                var httpResponse = await http.PostAsync(
                    $"{_facebookAPI}{postID}",
                    new FormUrlEncodedContent(postData)
                    );
                var httpContent = await httpResponse.Content.ReadAsStringAsync();

                return new Tuple<int, string>(
                    (int)httpResponse.StatusCode,
                    httpContent
                    );
            }
        }

        public async Task<List<CommentData>> GetCommentsByPostId(string postID)
        {
            List<CommentData> items = new List<CommentData>();
            using (var http = new HttpClient())
            {
                var httpResponse = await http.GetAsync($"{_facebookAPI}{postID}/comments?access_token={_accessToken}");

                if ((int)httpResponse.StatusCode == 200)
                {
                    var httpContent = await httpResponse.Content.ReadAsStringAsync();
                    FBComment com = JsonConvert.DeserializeObject<FBComment>(httpContent);
                    items = com.data.ToList();

                    while (com.paging.next != null)
                    {
                        httpResponse = await http.GetAsync(com.paging.next);
                        if ((int)httpResponse.StatusCode == 200)
                        {
                            httpContent = await httpResponse.Content.ReadAsStringAsync();
                            com = JsonConvert.DeserializeObject<FBComment>(httpContent);
                            items.AddRange(com.data.ToList());
                        }
                    }
                }

                List<CommentData> selectedItems = new List<CommentData>();

                foreach (var d in items)
                {
                    if (!selectedItems.Exists(x => x.from.id == d.from.id))
                    {
                        selectedItems.Add(d);
                    }
                }

                return selectedItems;
            }
        }

        public async Task<List<ReactData>> GetReactsByPostId(string postID)
        {
            List<ReactData> items = new List<ReactData>();
            using (var http = new HttpClient())
            {
                var httpResponse = await http.GetAsync($"{_facebookAPI}{postID}/reactions?access_token={_accessToken}");

                if ((int)httpResponse.StatusCode == 200)
                {
                    var httpContent = await httpResponse.Content.ReadAsStringAsync();
                    FBReact com = JsonConvert.DeserializeObject<FBReact>(httpContent);
                    items = com.data.ToList();

                    while (com.paging.next != null)
                    {
                        httpResponse = await http.GetAsync(com.paging.next);
                        if ((int)httpResponse.StatusCode == 200)
                        {
                            httpContent = await httpResponse.Content.ReadAsStringAsync();
                            com = JsonConvert.DeserializeObject<FBReact>(httpContent);
                            items.AddRange(com.data.ToList());
                        }
                    }
                }

                List<ReactData> selectedItems = new List<ReactData>();

                foreach (var d in items)
                {
                    if (!selectedItems.Exists(x => x.name == d.name))
                    {
                        selectedItems.Add(d);
                    }
                }

                return selectedItems;
            }
        }
        public async Task<Tuple<int, string>> PostComment(string commentText, string postId)
        {
            using (var http = new HttpClient())
            {
                var postData = new Dictionary<string, string> {
                { "access_token", _accessToken },
                { "message", commentText }//,
                // { "formatting", "MARKDOWN" } // doesn't work
            };

                var httpResponse = await http.PostAsync(
                    _facebookAPI + postId + "/comments",
                    new FormUrlEncodedContent(postData)
                    );
                var httpContent = await httpResponse.Content.ReadAsStringAsync();

                return new Tuple<int, string>(
                    (int)httpResponse.StatusCode,
                    httpContent
                    );
            }
        }
    }
}