using ProjectM;
using Unity.Entities;

namespace OpenRPG.Utils
{
    public static class Output
    {
        public static void SendLore(Entity userEntity, string message)
        {
            var user = Plugin.Server.EntityManager.GetComponentData<ProjectM.Network.User>(userEntity);
            ServerChatUtils.SendSystemMessageToClient(Plugin.Server.EntityManager, user, message);
        }
    }
}
