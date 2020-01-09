using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Kinect.BodyTracking;

namespace MotionTracking
{
    class PlayerManager
    {
        public Player LeadPlayer { get; private set; }
        private Queue<Player> otherPlayers = new Queue<Player>(); //when the lead player leaves, the player which was seen the earliest and wasnt a leader is choosen.
        //a queue of recent leaders could be easily added by using the updated boolean
        public void ConvertFrameToPlayerData(Frame frame)
        {
            double timestamp = frame.DeviceTimestamp.TotalMilliseconds;
            if (LeadPlayer == null)
            {
                LeadPlayer = AddNewPlayer(frame.GetBody(0),timestamp);
                Console.WriteLine("NEW LEADER CONSTRUCTED");
            }
            LeadPlayer.Updated = false; //we need to ensure the leadplayer has been updated. If the leadplayer is updated, it is changed back to true.
            for (uint i = 0; i < frame.NumberOfBodies; ++i) //how do we handle a new ID?
            {
              
                var body = frame.GetBody(i); 
                if(MatchPlayerWithID(LeadPlayer, body, timestamp))           
                    continue;           
                foreach(Player player in otherPlayers)
                {
                    if (MatchPlayerWithID(player, body, timestamp))                  
                        continue;                  
                }
                //since all existing players have been matched, an ID that was unable to be matched is turned into a new player and added to the queue
                //Event listeners are added to the player here.
                var newPlayer = AddNewPlayer(body, timestamp);
                otherPlayers.Enqueue(newPlayer);
            }
        }
        public void LeaderLogic()
        {
            uint firstleaderID = LeadPlayer.BodyID;

            if(LeadPlayer == null && otherPlayers.Count > 0)
            {
                LeadPlayer = otherPlayers.Dequeue();
            }
            while (!LeadPlayer.Updated)
                LeadPlayer = otherPlayers.Dequeue();
            foreach(Player player in otherPlayers)
            {
                if (player.BecomeLeader())
                {
                    LeadPlayer = player;               
                    //write deque logic to ensure player is not in other players
                }
            }
            uint lastleaderID = LeadPlayer.BodyID;
            if (lastleaderID != firstleaderID)
                Console.Write("lead player change");

        }
        public void DisposeAllPlayers()
        {
            LeadPlayer = null;
            otherPlayers = new Queue<Player>();
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
            return newPlayer;

        }
        public static  void c_CancelThersholdReached(object sender, EventArgs e)
        {
            Console.WriteLine("Cancel Threshold reached");
        }
        public void CheckEvents()
        {
            if (!LeadPlayer.PlayerMoving()) //we do not want the player to trigger events while moving.
            {
                LeadPlayer.AddCancelTrigger();
                
            }
        }

    }
    
}
