using Godot;
using IndexCities.Simulation;
namespace IndexCities.Game;

public partial class CityWorld
{
    static string VehicleVisual(Vehicle v)=>v.PassengerCar?"vehicle.car_passenger.a":v.Machine switch{
        PavingMachine.Paver=>"vehicle.asphalt_paver.a",PavingMachine.Roller=>"vehicle.road_roller.a",
        _=>v.Body switch{TruckBody.Dump=>"vehicle.truck_dump.a",TruckBody.Mixer=>"vehicle.truck_mixer.a",TruckBody.Flatbed=>"vehicle.truck_flatbed.a",_=>"vehicle.truck_cargo.a"}};

    static Node3D CreateVehicleVisual(Vehicle v)
    {
        var id=VehicleVisual(v);var file="res://Assets/Construction/Scenes/"+id+".tscn";
        var n=ResourceLoader.Exists(file)?GD.Load<PackedScene>(file)?.Instantiate<Node3D>():Scene(id);
        if(n==null){n=new();Box(n,new(2.3f,1,5.5f),new(0,.8f,0),v.Machine==PavingMachine.None?"bc864b":"d4aa3c");
            if(v.Machine==PavingMachine.None)Box(n,new(2.2f,1.6f,1.7f),new(0,1.8f,-1.8f),"6a93ad");
            if(v.Body==TruckBody.Mixer)n.AddChild(new MeshInstance3D{Mesh=new SphereMesh{Radius=1.2f,Height=2.6f},Position=new(0,2,.6f),MaterialOverride=Material("e4dbb6")});
            else if(v.Body==TruckBody.Dump)Box(n,new(2.3f,1,3.7f),new(0,1.7f,.7f),"c3aa72");
        }
        n.SetMeta("vehicle_visual",id);if(v.PassengerCar)PaintCar(n,v.Id);return n;
    }

    void SyncVehicleVisuals(CityState state,bool interpolate,double? interpolation)
    {
        foreach(var v in state.Vehicles){
            if(v.Status==VehicleStatus.Outside){Remove(_trucks,v.Id);continue;}
            if(_trucks.TryGetValue(v.Id,out var old)&&old.GetMeta("vehicle_visual","").AsString()!=VehicleVisual(v))Remove(_trucks,v.Id);
            if(!_trucks.TryGetValue(v.Id,out var n)){n=CreateVehicleVisual(v);n.Name="Vehicle"+v.Id;AddChild(n);_trucks[v.Id]=n;}
            var pose=interpolate&&_vehicleMotion.TryGetValue(v.Id,out var motion)?motion.At(interpolation!.Value):(v.Position,(double?)null);
            var travel=n.GetMeta("wheel_travel",0.0).AsDouble();if(n.HasMeta("last_position"))travel+=n.GetMeta("last_position").AsVector2().DistanceTo(new((float)pose.Item1.X,(float)pose.Item1.Z));n.SetMeta("last_position",new Vector2((float)pose.Item1.X,(float)pose.Item1.Z));n.SetMeta("wheel_travel",travel);
            n.Position=P(pose.Item1,RoadSurfaceAt(pose.Item1));n.Rotation=new(0,(float)(pose.Item2??v.Heading+Math.PI),0);
            SyncConstructionParts(n,v,state);
        }
    }

    static void SyncConstructionParts(Node3D n,Vehicle v,CityState state)
    {
        // The visible quantity derives from cargo still on the vehicle. Delivery
        // consumes it once, in the core; no visual callback changes the stock.
        var shipment=state.Shipments.FirstOrDefault(s=>s.Id==v.ShipmentId);
        var unloading=v.Status==VehicleStatus.Unloading&&v.RefuellingAt==null&&shipment is {Complete:false,Cancelled:false,PickedUp:true}&&v.Cargo.Values.Sum()>.001;
        if(shipment?.ReceivingMachineId is {} receiverId)unloading=unloading&&state.Vehicles.Any(receiver=>receiver.Id==receiverId&&receiver.MachinePhase==MachinePhase.OnSite&&receiver.FeedingReady&&receiver.CrewAboard&&receiver.CrewCount>0&&receiver.Fuel>0&&receiver.Position.Distance(v.Position)<=7);
        var fraction=Math.Clamp(v.Cargo.Values.Sum()/Math.Max(1,v.CargoCapacity),0,1);
        foreach(var (kind,path) in new[]{(ResourceKind.Aggregate,"BedPivot/LoadSandGravel"),(ResourceKind.Asphalt,"BedPivot/LoadAsphalt"),(ResourceKind.Steel,"LoadSteel"),(ResourceKind.Wood,"LoadWood")}){
            var load=n.GetNodeOrNull<Node3D>(path);
            if(load==null)continue;var quantity=v.Cargo.GetValueOrDefault(kind);load.Visible=quantity>.001;
            load.Scale=new(1,(float)Math.Max(.02,quantity/Math.Max(1,v.CargoCapacity)),1);
        }
        if(n.GetNodeOrNull<Node3D>("BedPivot/Cover") is {} cover)cover.Visible=v.Cargo.GetValueOrDefault(ResourceKind.Asphalt)>.001&&!unloading;
        var tipping=unloading?Math.Sin(Math.Clamp((15-v.LoadTimer)/15,0,1)*Math.PI):0;
        if(n.GetNodeOrNull<Node3D>("BedPivot") is {} dump)dump.Rotation=new((float)(tipping*Math.PI*38/180),0,0);
        if(n.GetNodeOrNull<Node3D>("BedPivot/TailgatePivot") is {} tailgate)tailgate.Rotation=new((float)(-tipping*Math.PI*72/180),0,0);
        if(n.GetNodeOrNull<Node3D>("HydraulicAnchor") is {} anchor&&n.GetNodeOrNull<Node3D>("BedPivot/HydraulicTarget") is {} hydraulicTarget){
            var start=anchor.Position;var delta=n.ToLocal(hydraulicTarget.GlobalPosition)-start;var length=delta.Length();
            if(length>.001f)foreach(var (path,begin,partFraction,originalLength) in new[]{("HydraulicBase",0f,.48f,.48f),("HydraulicRod",.48f,.52f,.4f)})if(n.GetNodeOrNull<Node3D>(path) is {} part){part.Quaternion=new Quaternion(Vector3.Up,delta.Normalized());part.Position=start+delta*(begin+partFraction/2);part.Scale=new(1,length*partFraction/originalLength,1);}
        }
        if(n.GetNodeOrNull<Node3D>("DrumMount/Drum") is {} drum)drum.Rotation=new(0,0,unloading?(float)(state.ElapsedSeconds*1.7%Math.Tau):0);
        if(n.GetNodeOrNull<Node3D>("ChutePivot") is {} chute)chute.Rotation=new(0,unloading?.4f:0,0);
        var travel=n.GetMeta("wheel_travel",0.0).AsDouble();
        foreach(var wheel in new[]{"Wheel_FL","Wheel_FR","Wheel_ML","Wheel_MR","Wheel_RL","Wheel_RR"})if(n.GetNodeOrNull<Node3D>(wheel) is {} part)part.Rotation=new((float)(-travel/.47%Math.Tau),0,0);
        foreach(var roller in new[]{"FrontSteering/FrontDrum","RearDrum"})if(n.GetNodeOrNull<Node3D>(roller) is {} part)part.Rotation=new((float)(-travel/.5885%Math.Tau),0,0);
        var work=state.Projects.FirstOrDefault(p=>p.Id==v.MachineProjectId);
        var working=v.MachinePhase==MachinePhase.OnSite&&v.WorkTarget!=null&&v.CrewAboard&&v.FuelBurnedLastStep>0&&work?.Cause==WorkCause.PavingInProgress;
        if(n.GetNodeOrNull<Node3D>("ScreedPivot") is {} screed)screed.Position=new(screed.Position.X,working?.42f:.5f,screed.Position.Z);
        if(n.GetNodeOrNull<Node3D>("HopperLoad") is {} hopper){var remaining=work==null?0:Math.Max(0,1-work.AppliedRoadPoints.Count/(double)work.RoadPoints.Count);hopper.Visible=(working||v.MachinePhase==MachinePhase.OnSite&&v.FeedingReady&&work?.Delivered.GetValueOrDefault(ResourceKind.Asphalt)>0)&&remaining>0;hopper.Scale=new(1,(float)Math.Max(.02,remaining),1);}
        n.SetMeta("working",working);
        n.SetMeta("cargo_fraction",fraction);n.SetMeta("unloading",unloading);
    }

    public string RenderedVehicle(int id)=>_trucks.TryGetValue(id,out var node)?node.GetMeta("vehicle_visual","").AsString():"";

    void DrawAppliedAsphalt(Node3D root,WorkProject work,CityState state)
    {
        var layout=work.RoadPoints.Concat(state.Roads.Select(r=>r.Position)).ToHashSet();
        var step=(float)state.Tuning.RoadStep;var lane=(float)state.Tuning.RoadWidth*(6.5f/10.5f);
        foreach(var point in work.AppliedRoadPoints.Where(point=>state.Roads.All(r=>r.Position!=point))){
            var axes=RoadGeometry.Directions(point,layout,step);
            var size=axes.Horizontal&&axes.Vertical?new Vector3(lane,.045f,lane):axes.Vertical?new Vector3(lane,.045f,step):new Vector3(step,.045f,lane);
            Box(root,size,P(point,.04f),"3e4345");
        }
    }
}
