using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Kinect.BodyTracking;

namespace MotionTracking
{
    class PlayerManager
    {
        private Player LeadPlayer;
        private Queue<Player> OtherPlayers = new Queue<Player>(); //when the lead player leaves, the player which was seen the earliest and wasnt a leader is choosen.
        private readonly string start = "_START_";
        private readonly string end = "_END_";
        private uint playerCount = 0;
        private uint previousPlayerCount = 0;
        //a queue of recent leaders could be easily added by using the updated boolean
        public void ConvertFrameToPlayerData(Frame frame)
        {
            playerCount = frame.NumberOfBodies;
            if (playerCount != 0)
            {
                double timestamp = frame.DeviceTimestamp.TotalMilliseconds;
                playerCount = frame.NumberOfBodies;
                if (LeadPlayer == null)
                {
                    LeadPlayer = AddNewPlayer(frame.GetBody(0), timestamp);
                    Console.WriteLine("NEW LEADER CONSTRUCTED");
                }
                LeadPlayer.Updated = false; //we need to ensure the leadplayer has been updated. If the leadplayer is updated, it is changed back to true.
                for (uint i = 0; i < frame.NumberOfBodies; ++i) //how do we handle a new ID?
                {

                    var body = frame.GetBody(i);
                    if (MatchPlayerWithID(LeadPlayer, body, timestamp))
                        continue;
                    foreach (Player player in OtherPlayers)
                    {
                        if (MatchPlayerWithID(player, body, timestamp))
                            continue;
                    }
                    //since all existing players have been matched, an ID that was unable to be matched is turned into a new player and added to the queue
                    //Event listeners are added to the player here.
                    var newPlayer = AddNewPlayer(body, timestamp);
                    OtherPlayers.Enqueue(newPlayer);
                }
            }
            if(previousPlayerCount != 0 && playerCount == 0)
            {
                AllPlayersGonePrint();
            }
            previousPlayerCount = playerCount;
        }

        public void LeaderLogic()
        {
            uint firstleaderID = LeadPlayer.BodyID;
            bool manualLeaderChangePerformed = false;
            bool trigonce = true;

            if(LeadPlayer == null && OtherPlayers.Count > 0)
            {
                LeadPlayer = OtherPlayers.Dequeue();
            }
            if (LeadPlayer.Updated && LeadPlayer.BecomeLeader())//TODO test comprehensively.
                return;
            while (!LeadPlayer.Updated)
                LeadPlayer = OtherPlayers.Dequeue();
            foreach(Player player in OtherPlayers)
            {
                if (player.BecomeLeader())
                {
                    LeadPlayer = player;
                    RemovePlayerFromQueue(LeadPlayer); 
                    manualLeaderChangePerformed = true;
                    
                    //write deque logic to ensure player is not in other players
                }
                else
                {
                    player.StopGestureActivation(); //stops any other players from gesture triggering. Test.
                }
                if(manualLeaderChangePerformed && trigonce)
                {
                    trigonce = false;
                    Console.WriteLine("_START__PLAYSOUND_ tpose_END_");
                }
            }
            uint lastleaderID = LeadPlayer.BodyID;
            if (lastleaderID != firstleaderID)
                Console.Write("lead player change");

        }

        public void DisposeAllPlayers()
        {
            LeadPlayer = null;
            OtherPlayers= new Queue<Player>();
        }       
        private bool MatchPlayerWithID(Player player, Body body, double timestamp)
        {
            var rightMatch = player.BodyID == body.Id;
            if (rightMatch)
            {
                player.UpdatePlayer(body, timestamp);
            }
            return rightMatch;
        }
        private Player AddNewPlayer(Body body, double timestamp) //creates a new player with all necessary event listeners.
        {
            var newPlayer = new Player(body, timestamp);
            newPlayer.CancelThresholdReached += c_CancelThersholdReached;
            newPlayer.ZoomThresholdReached += c_ZoomThresholdReached;
            newPlayer.ZoomThresholdReached += c_OrbitThresholdReached; // orbit and zoom are currently bound to the same position.
            newPlayer.ZoomSoundThresholdReached += c_ZoomSoundThresholdReached;
            return newPlayer;

        }
        private void RemovePlayerFromQueue(Player player)
        {
            var playerHolder = new Queue<Player>();
            while(OtherPlayers.Count != 0)
            {
                if (OtherPlayers.Dequeue().Equals(player))
                    OtherPlayers.Dequeue();
                else
                {
                    playerHolder.Enqueue(OtherPlayers.Dequeue());
                }
            }
           while(playerHolder.Count != 0)
            {
                OtherPlayers.Enqueue(playerHolder.Dequeue());
            }

        }
        public void c_CancelThersholdReached(object sender, EventArgs e)
        {
            Console.WriteLine("_START__PLAYSOUND_ stop_END_");
            LeadPlayer.StopGestureActivation();
        }
        public void c_ZoomThresholdReached(object sender, EventArgs e)//set private?
        {        
            Console.WriteLine("{0}{1}{2}{3}",start,"_EXPLODE_ ",LeadPlayer.WristToWristDistance(),end);
        }
        public void c_OrbitThresholdReached(object sender, EventArgs e)
        {
            Console.WriteLine("{0}{1}{2}{3}",start,"_ORBIT_ ",LeadPlayer.HandPositionsString(),end);
        }
        public void c_ZoomSoundThresholdReached(object sender, EventArgs e)
        {
            Console.WriteLine("_START__PLAYSOUND_ manipulate_END_");
            Console.WriteLine("_START__MANIPULATE__END_");
        }
        private void EyePositionPrint()
        {
            Console.WriteLine("{0}{1}{2}{4}", start, "_HEADPOSITION_ ", LeadPlayer.ImprovedVectorToString(LeadPlayer.AverageEyesPosition()), end);
        }
        private void AllPlayersGonePrint()
        {
            Console.WriteLine("_START__PLAYSOUND_ noplayers_END_");
            Console.WriteLine("_START__RESET__END_");
        }
        public void CheckEvents()
        {
            EyePositionPrint();
            if (!LeadPlayer.PlayerMoving()) //we do not want the player to trigger events while moving.
            {
                LeadPlayer.AddCancelTrigger();
                LeadPlayer.AddZoomTrigger();
            }

        }

    }
    
}
