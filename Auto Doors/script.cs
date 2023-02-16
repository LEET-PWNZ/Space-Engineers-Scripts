/*
 * R e a d m e
 * -----------
 * 
 * In this file you can include any instructions or other comments you want to have injected onto the 
 * top of your final script. You can safely delete this file if you do not want any such comments.
 */

/*
  *   R e a d m e
  *   -----------
  *   This script can be used as-is, and it is simple to understand and modify. Simply upload it to a Programmable block and compile. (No timer block needed)
  *   It automatically closes all the doors on the local grid, after a specified period (default 3 secs) has elapsed from the moment the door was opened.
  *
  *   If you want to increase / decrease the close delay, pass a 2nd parameter of type int, to the constructor of the 'DoorManager' class (or just change the hard-coded value). The default is 3 seconds. Eg: `doorManager = new DoorManager(this,5);` This will set the close delay to 5 seconds.
  *   If you want the script to auto-close doors on connected grids as well, then pass a 3rd parameter of type bool, to the constructor of the 'DoorManager' class (or just change the hard-coded value). The default is false. Eg: `doorManager = new DoorManager(this,3,true);` This will also auto-close doors on connected grids.
  *   The bulk of this script was put into a class so that it can be easily combined with existing scripts.
  *
  *   And that is all there is to it. Enjoy your auto-closing doors!
  *
  *   Author: Stuyvenstein
  *
  *   Thanks to malware-dev for the awesome MDK!
  *
  */
private DoorManager doorManager;

public Program()
{
    doorManager = new DoorManager(this);
    Runtime.UpdateFrequency = UpdateFrequency.Update10;
}

public void Save()
{

}

public void Main(string argument)
{
    doorManager?.Run();
}

}
public class DoorManager
{
    private List<AutoDoor> AutoDoors = new List<AutoDoor>();
    private int _CloseDelaySeconds;
    private bool _AffectConnectedGrids;
    private MyGridProgram ParentProgram;
    private int ElapsedTicks = 0;


    public DoorManager(MyGridProgram callingProgram, int CloseDelaySeconds = 3, bool AffectConnectedGrids = false)
    {
        _CloseDelaySeconds = CloseDelaySeconds;
        _AffectConnectedGrids = AffectConnectedGrids;
        ParentProgram = callingProgram;
        UpdateDoorList();
    }

    //This function maintains the list of doors to be auto-closed, and executes every 300 ticks
    private void UpdateDoorList()
    {
        List<IMyDoor> allGridDoors = new List<IMyDoor>();
        //Gets all the doors on the local, and all the connected grids
        if (_AffectConnectedGrids) ParentProgram.GridTerminalSystem.GetBlocksOfType<IMyDoor>(allGridDoors, null);
        //Gets all the doors on the current grid only
        else ParentProgram.GridTerminalSystem.GetBlocksOfType<IMyDoor>(allGridDoors, d => d.CubeGrid == ParentProgram.Me.CubeGrid);

        //Add new doors
        foreach (IMyDoor gridDoor in allGridDoors)
        {
            if (AutoDoors.Where(d => d.doorRef == gridDoor).ToList().Count == 0)
            {
                AutoDoor autoDoor = new AutoDoor();
                autoDoor.doorRef = gridDoor;
                AutoDoors.Add(autoDoor);
            }
        }

        //Remove missing doors
        foreach (AutoDoor autoDoor in AutoDoors)
        {
            if (!allGridDoors.Contains(autoDoor.doorRef)) AutoDoors.Remove(autoDoor);
        }
    }

    public void Run()
    {
        foreach (AutoDoor autoDoor in AutoDoors)
        {
            //Find open doors that aren't yet flagged for auto-closing
            if (autoDoor.doorRef?.Status == (DoorStatus.Open | DoorStatus.Opening) && !autoDoor.IsTiming)
            {
                //Flag door for auto-closing
                autoDoor.IsTiming = true;
                autoDoor.TimeOpened = DateTime.Now;
            }

            //Check if opened door has reached or passed the door close delay period, and closes it if true
            if (autoDoor.doorRef?.Status == (DoorStatus.Open | DoorStatus.Opening) && autoDoor.IsTiming)
            {
                if (DateTime.Now.Subtract(autoDoor.TimeOpened).Seconds >= _CloseDelaySeconds)
                {
                    autoDoor.doorRef.CloseDoor();
                    autoDoor.IsTiming = false;
                }
            }

            //Handle manually closed doors
            if (autoDoor.doorRef?.Status == (DoorStatus.Closed | DoorStatus.Closing) && autoDoor.IsTiming) autoDoor.IsTiming = false;
        }

        //This script runs every 10 ticks, and we want to update the door list every 300 ticks, hence the below
        if (ElapsedTicks == 30)
        {
            UpdateDoorList();
            ElapsedTicks = 0;
        }
        else
        {
            ElapsedTicks++;
        }
    }

    public class AutoDoor
    {
        public IMyDoor doorRef;
        public bool IsTiming = false;
        public DateTime TimeOpened;
    }