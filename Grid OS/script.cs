
private ScreenManager consoleScreen;
private TickEventManager tickEventManager;
private static float sunSpeedRpm = 1f / 120f;
private EnergyDiagSystem energyDiagSystem;

private IMyMotorStator sunTrackRotor;
private IMyMotorAdvancedStator hingeH,hingeV;
public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update1;
    tickEventManager = new TickEventManager();
    energyDiagSystem = new EnergyDiagSystem(this, tickEventManager);
    tickEventManager.OnEventTick += tmpTick;
    tickEventManager.OnEventTick10 += TickEventManager_OnEventTick10;
    tickEventManager.OnEventTick100 += TickEventManager_OnEventTick100;
    consoleScreen = new ScreenManager(this, "Console LCD");
    UpdateBlocks();
}

public void Main(string argument, UpdateType updateSource)
{
    tickEventManager.UpdateTickManager();

}

private void tmpTick()
{
    if (sunTrackRotor != null && sunTrackRotor.IsWorking )
    {
        consoleScreen.UpdateScreen();
        var rotorSpeed = 0f;
        if (sunTrackRotor.Angle.ToString("0") != 0.ToString("0"))
        {
            if (sunTrackRotor.Angle < 0f) rotorSpeed = 2f;
            else rotorSpeed = -2f;
        }
        sunTrackRotor.TargetVelocityRPM = rotorSpeed;
    }
    var angleRadian = 0.0174533f;
    var hingeSpeed = 0.3f;
    if (hingeH != null && hingeH.IsWorking)
    {
        var horAngle = -(35f * angleRadian);
        var hspeed = 0f;
        if (hingeH.Angle != horAngle)
        {
            if (hingeH.Angle < horAngle) hspeed = hingeSpeed;
            else hspeed = -hingeSpeed;
        }
        hingeH.TargetVelocityRPM = hspeed;
    }
    if (hingeV != null && hingeV.IsWorking)
    {
        var verAngle = (35f * angleRadian);
        var vspeed = 0f;
        if (hingeV.Angle != verAngle)
        {
            if (hingeV.Angle < verAngle) vspeed = hingeSpeed;
            else vspeed = -hingeSpeed;
        }
        hingeV.TargetVelocityRPM = vspeed;
    }
}

private void TickEventManager_OnEventTick()
{
    consoleScreen.UpdateScreen();
    float rotorSpeedRpm=sunSpeedRpm;
    if (energyDiagSystem.solarPanelManager.PanelEfficiency  < 95f)
    {
        rotorSpeedRpm = 2f;
    }
    rotorSpeedRpm = sunSpeedRpm;
    if (sunTrackRotor!=null&& sunTrackRotor.IsWorking && sunTrackRotor.TargetVelocityRPM != rotorSpeedRpm)
    {
        sunTrackRotor.TargetVelocityRPM = rotorSpeedRpm;
    }
    var angleRadian = 0.0174533f;
    var hingeSpeed = 0.3f;
    if (hingeH!=null &&hingeH.IsWorking)
    {
        var horAngle = -(45f* angleRadian);
        var hspeed = 0f;
        if(hingeH.Angle != horAngle)
        {
            if (hingeH.Angle < horAngle) hspeed = hingeSpeed;
            else hspeed = -hingeSpeed;
        }
        hingeH.TargetVelocityRPM = hspeed;
    }
    if (hingeV != null && hingeV.IsWorking)
    {
        var verAngle = (45f* angleRadian);
        var vspeed = 0f;
        if (hingeV.Angle != verAngle)
        {
            if (hingeV.Angle < verAngle) vspeed = hingeSpeed;
            else vspeed = -hingeSpeed;
        }
        hingeV.TargetVelocityRPM = vspeed;
    }
}

private void TickEventManager_OnEventTick10()
{
    var consoleOutput = sunTrackRotor.Angle.ToString("0.00");
    if (hingeH != null && hingeH.IsWorking)
    {
        consoleOutput += "\nHinge H " + hingeH.Angle.ToString("0.00");
    }
    if (hingeV != null && hingeV.IsWorking)
    {
        consoleOutput += "\nHinge V " + hingeV.Angle.ToString("0.00");
    }
    consoleScreen.WriteAllText(consoleOutput);
}

private void TickEventManager_OnEventTick100()
{
    UpdateBlocks();

    consoleScreen.UpdateScreenBlock();
}

private void UpdateBlocks()
{
    sunTrackRotor = GridTerminalSystem.GetBlockWithName("Sun Rotor") as IMyMotorStator;
    hingeH= GridTerminalSystem.GetBlockWithName("Solar Hinge H") as IMyMotorAdvancedStator;
    hingeV = GridTerminalSystem.GetBlockWithName("Solar Hinge V") as IMyMotorAdvancedStator;
}

public void Save()
{

}

}
public class BatteryManager
{
    private MyGridProgram batteryParentProgram;
    Func<IMyBatteryBlock, bool> blockFilter;
    private List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    public static float BaseCapacityMw = 3f;
    public float TotalCapacityMw = 0f, TotalChargeMw = 0f, TotalDrainMw = 0f, MaxChargeMw = 0f, MaxDrainMw = 0f;
    public int WorkingBatteryCount=0;

    public float MaxCapacityMw { get { return BaseCapacityMw * WorkingBatteryCount; } }

    public BatteryManager(MyGridProgram inParentProgram, Func<IMyBatteryBlock, bool> filter = null)
    {
        batteryParentProgram = inParentProgram;
        blockFilter = filter;
        UpdateBatteryBlocks();
        UpdateBatteryData();
    }


    public void UpdateBatteryBlocks()
    {
            batteryParentProgram.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries, blockFilter);
    }

    public void UpdateBatteryData()
    {
        var workingBats = batteries.Where(b => b.IsWorking).ToList();
        WorkingBatteryCount = workingBats.Count;
        if (WorkingBatteryCount > 0)
        {
            TotalCapacityMw = workingBats.Sum(b => b.CurrentStoredPower);
            TotalDrainMw = workingBats.Sum(b => b.CurrentOutput);
            TotalChargeMw = workingBats.Sum(b => b.CurrentInput);
            MaxChargeMw = workingBats.Sum(b => b.MaxInput);
            MaxDrainMw = workingBats.Sum(b => b.MaxOutput);
        }
    }

}

public class EnergyDiagSystem
{
    private readonly MyGridProgram _parentProgram;
    public const string ScreenName = "Energy Info LCD";
    public ScreenManager energyScreen;
    public SolarPanelManager solarPanelManager;
    public BatteryManager batteryManager;
    public EnergyDiagSystem(MyGridProgram parentProgram, TickEventManager tickEventManager)
    {
        _parentProgram = parentProgram;
        energyScreen = new ScreenManager(_parentProgram,ScreenName);
        solarPanelManager = new SolarPanelManager(_parentProgram);
        batteryManager = new BatteryManager(_parentProgram, b => b.CubeGrid == _parentProgram.Me.CubeGrid);
        tickEventManager.OnEventTick += diagTick;
        tickEventManager.OnEventTick10 += diagTick10;
        tickEventManager.OnEventTick100 += diagTick100;
    }

    private void diagTick()
    {
        solarPanelManager.UpdateSolarData();
        batteryManager.UpdateBatteryData();
        energyScreen.UpdateScreen();
    }

    private void diagTick10()
    {
        var totalSolarPotential = solarPanelManager.PanelPotentialMw * solarPanelManager.WorkingPanelCount;
        var solarBaseMax = SolarPanelManager.PanelBaseMw * solarPanelManager.WorkingPanelCount;
        var powerInfo = "Solar Efficiency: "+ totalSolarPotential.ToString("0.00") + " MW / " + solarBaseMax.ToString("0.00") + " MW (" + solarPanelManager.PanelEfficiency.ToString("0") + "%)";
        powerInfo += "\nSolar Drain: " + solarPanelManager.TotalDrainMw.ToString("0.00") + " MW / " + totalSolarPotential.ToString("0.00") + " MW";
        powerInfo += "\nWorking Panel Count: " + solarPanelManager.WorkingPanelCount.ToString();
        powerInfo += "\nBattery Count: " + batteryManager.WorkingBatteryCount.ToString();
        powerInfo += "\nBattery Capacity: " + batteryManager.TotalCapacityMw.ToString("0.00") + "MW / " + batteryManager.MaxCapacityMw.ToString("0.00") + "MW (" + ((batteryManager.TotalCapacityMw / batteryManager.MaxCapacityMw) * 100f).ToString("0")+"%)";
        powerInfo += "\nBattery Charge: " + batteryManager.TotalChargeMw.ToString("0.00") + "MW / " + batteryManager.MaxChargeMw.ToString("0.00") + "MW";
        powerInfo += "\nBattery Discharge: " + batteryManager.TotalDrainMw.ToString("0.00") + "MW / " + batteryManager.MaxDrainMw.ToString("0.00") + "MW";
        energyScreen.WriteAllText(powerInfo);
    }

    private void diagTick100()
    {
        solarPanelManager.UpdateSolarBlocks();
        batteryManager.UpdateBatteryBlocks();
        energyScreen.UpdateScreenBlock();
    }

}

public class ScreenManager
{
    private MyGridProgram screenParentProgram;
    private IMyTextPanel textPanel = null;
    private List<string> screenLines = new List<string>();
    private string _screenName;
    private string _screenText;
    public ScreenManager(MyGridProgram inParentProgram,string screenName)
    {
        screenParentProgram = inParentProgram;
        _screenName = screenName;
        UpdateScreenBlock();
        WriteAllText("");
        UpdateScreen();
    }

    public void UpdateScreenBlock()
    {
        var textPanels = new List<IMyTextPanel>();
        screenParentProgram.GridTerminalSystem.GetBlocksOfType(textPanels, b=> b.CustomName.Equals(_screenName) && b.CubeGrid==screenParentProgram.Me.CubeGrid);
        textPanel = textPanels.FirstOrDefault();
        if(textPanel!=null) textPanel.ContentType = ContentType.TEXT_AND_IMAGE;
        //textPanel?.ShowPublicTextOnScreen();
    }

    public void UpdateScreen()
    {

           textPanel?.WriteText(_screenText);
    }

    public void WriteLine(string line)
    {
        _screenText += line+ "\n";
    }

    public void WriteAllText (string text)
    {
        _screenText = text;
    }

}

public class SolarPanelManager
{
    private MyGridProgram solarParentProgram;
    private Func<IMySolarPanel, bool> blockFilter;
    private List<IMySolarPanel> solarPanels = new List<IMySolarPanel>();
    public static float PanelBaseMw = 160f/1000f;
    public float PanelPotentialMw=0f,TotalDrainMw=0f;
    public int WorkingPanelCount=0;
    public float PanelEfficiency { get { return (PanelPotentialMw / PanelBaseMw) * 100f; } }

    public SolarPanelManager(MyGridProgram inParentProgram, Func<IMySolarPanel, bool> filter=null)
    {
        solarParentProgram = inParentProgram;
        blockFilter = filter;
        UpdateSolarBlocks();
        UpdateSolarData();
    }

    public void UpdateSolarBlocks()
    {
            solarParentProgram.GridTerminalSystem.GetBlocksOfType<IMySolarPanel>(solarPanels, blockFilter);
    }

    public void UpdateSolarData()
    {
        var workingPanels = solarPanels.Where(p => p.IsWorking).ToList();
        WorkingPanelCount = workingPanels.Count;
        if (WorkingPanelCount > 0)
        {
            TotalDrainMw = workingPanels.Sum(p => p.CurrentOutput);
            PanelPotentialMw= workingPanels.Sum(p => p.MaxOutput) / WorkingPanelCount;
        }
    }

}

public class TickEventManager
{
    public delegate void OnTickEvent();
    public event OnTickEvent OnEventTick;
    public event OnTickEvent OnEventTick10;
    public event OnTickEvent OnEventTick100;
    private int tickCount100=0;
    private int tickCount10 = 0;

    public void UpdateTickManager()
    {
        OnEventTick?.Invoke();
        tickCount10++;
        tickCount100++;
        if (tickCount10 >= 10)
        {
            OnEventTick10?.Invoke();
            tickCount10 = 0;
        }
        if (tickCount100 >= 100)
        {
            OnEventTick100?.Invoke();
            tickCount100 = 0;
        }
    }