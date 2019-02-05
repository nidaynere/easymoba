/*! 
@author EasyMOBA <easymoba.com>
@lastupdate 24 December 2017
*/

using UnityEngine;
using UnityEngine.Networking;

public class UNETServer : MonoBehaviour
{
    public static void StartHost()
    {
        if (NetworkServer.Listen(ServerManager.setting.port))
        {
            Debug.Log("Server is listening");

            NetworkServer.RegisterHandler(MsgType.Connect, ServerManager.current.ClientConnected);
            NetworkServer.RegisterHandler(MsgType.Disconnect, ServerManager.current.ClientDisconnected);
            NetworkServer.RegisterHandler(MTypes.FindGameRequest, ServerManager.current.FindGameRequest);
            NetworkServer.RegisterHandler(MTypes.MoveRequest, ServerManager.current.MoveRequest);
            NetworkServer.RegisterHandler(MTypes.SkillRequest, ServerManager.current.SkillRequest);
            NetworkServer.RegisterHandler(MTypes.AimRequest, ServerManager.current.AimRequest);
            NetworkServer.RegisterHandler(MTypes.LastAim, ServerManager.current.LastAim);
            NetworkServer.RegisterHandler(MTypes.HeroChangeRequest, ServerManager.current.HeroChangeRequest);
            NetworkServer.RegisterHandler(MTypes.BotRequest, ServerManager.current.BotRequest);
        }
    }
}
