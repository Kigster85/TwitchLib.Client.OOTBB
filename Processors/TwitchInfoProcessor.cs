using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TwitchBot.ApiControllers;
using TwitchBot.Models;
using System.Text.Json; // Make sure to include this

namespace TwitchBot.Processors
{
  public class TwitchInfoProcessor
  {
    public async Task<TwitchInfoModel> TwitchOauthInfo(string TwitchAuthInfo)
    {
      using (HttpResponseMessage response = await ApiHandler.ApiServer.GetAsync(TwitchAuthInfo))
      {
        if (response.IsSuccessStatusCode)
        {
          TwitchInfoModel twitchInfoModel = await JsonSerializer.DeserializeAsync<TwitchInfoModel>(await response.Content.ReadAsStreamAsync());

          return twitchInfoModel;
        }
        else
        {
          throw new HttpRequestException(response.ReasonPhrase);
        }
      }
    }
  }
}
