using Discord;
using Discord.WebSocket;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Agatha2
{
	internal class CommandTwitch : BotCommand
	{
		internal CommandTwitch()
		{
			usage = "twitch";
			description = "Look up a Twitch streamer.";
			aliases = new List<string>() {"twitch"};
		}
		internal override async Task ExecuteCommand(SocketMessage message, GuildConfig guild)
		{
			ModuleTwitch twitch = (ModuleTwitch)parent;
			string[] message_contents = message.Content.Substring(1).Split(" ");
			if(message_contents.Length >= 2)
			{
				string streamer = message_contents[1];
				JToken jData = twitch.RetrieveUserIdFromUserName(streamer);
				if(jData != null && jData.HasValues)
				{
					var request = (HttpWebRequest)WebRequest.Create($"https://api.twitch.tv/helix/streams?user_id={twitch.streamNametoID[streamer]}");
					request.Method = "Get";
					request.Timeout = 12000;
					request.ContentType = "application/vnd.twitchtv.v5+json";
					request.Headers.Add("Client-ID", twitch.streamAPIClientID);

					try
					{
						JToken jsonStream = null;
						using (var s = request.GetResponse().GetResponseStream())
						{
							using (var sr = new System.IO.StreamReader(s))
							{
								var jsonObject = JObject.Parse(sr.ReadToEnd());
								var tmp = jsonObject["data"];
								if(tmp.HasValues)
								{
									jsonStream = tmp[0];
								}
							}
						}
						await Program.SendReply(message, twitch.MakeAuthorEmbed(jData, jsonStream));
					}
					catch(WebException e)
					{
						Program.WriteToLog($"Stream exception: {e}");
					}
				}
				else
				{
					 await Program.SendReply(message, $"No user found for '{streamer}'.");
				}
			}
			else
			{
				twitch.PollStreamers(message);
				await Program.SendReply(message, $"Subscribed streamer polling complete.");
			}
		}
	}
}