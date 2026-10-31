using System.Text.Json.Serialization;

namespace IndexCities.Simulation;

public readonly record struct Point(double X, double Z)
{
    public double Distance(Point b) => Math.Sqrt((X-b.X)*(X-b.X)+(Z-b.Z)*(Z-b.Z));
    public Point MoveTowards(Point b, double distance) { var d=Distance(b); return d<1e-9||distance>=d?b:new(X+(b.X-X)*distance/d,Z+(b.Z-Z)*distance/d); }
    public static Point operator +(Point a, Point b)=>new(a.X+b.X,a.Z+b.Z);
    public static Point operator -(Point a, Point b)=>new(a.X-b.X,a.Z-b.Z);
    public static Point operator *(Point p,double scale)=>new(p.X*scale,p.Z*scale);
}
public enum BuildingKind { CityHall, WorksDepot, House, Grocery, Farm, FuelStation, WaterIntake, WaterTreatment, SewageTreatment, WindPower, MechanicShop }
public enum ResourceKind { Aggregate, Concrete, Steel, Wood, Asphalt, Food, Fuel }
public enum WorkStatus { Waiting, InProgress, Completed, Cancelled }
public enum JourneyPurpose { Arriving, Work, Home, Shopping, Leaving, Construction, Boarding, Visit, Stroll, Service }
public enum VehicleStatus { Outside, Travelling, Unloading, Parked, Waiting, Stranded, Towed }
public enum ShipmentPurpose { Construction, Food, Fuel, Vehicle, Maintenance }
public enum TruckBody { Cargo, Dump, Mixer, Flatbed, Tow, TowHeavy }
// Tow service from the exterior (SPEC transporte-circulacao, Socorro a veículos parados).
public enum TowPhase { None, ToVehicle, Loading, Delivering, Unloading }
public enum TowDestination { Station, Exterior, Holding, Shop, Home }
// Breakdowns, repair and revision (SPEC transporte-circulacao, "Quebras, guincho local e oficina").
public enum ServiceKind { None, Repair, Revision }
public enum ServicePhase { None, ToService, Queued, InService, AwaitingReturn, AtExterior, AwaitingMoney }
public enum PavingMachine { None, Paver, Roller }
public enum MachinePhase { Available, ToSite, OnSite, LeavingSite, Returning }
public enum JunctionControl { RightHand, ThroughX, ThroughZ, Signal }

public sealed record BuildingDefinition(BuildingKind Kind,string Name,double Width,double Depth,decimal BaseValue,int Jobs,double Capacity,double WorkSeconds,Dictionary<ResourceKind,double> Materials)
{
    public bool Private=>Kind is BuildingKind.House or BuildingKind.Grocery or BuildingKind.Farm or BuildingKind.FuelStation or BuildingKind.MechanicShop;
    public int TaxCategory=>Kind==BuildingKind.House?0:Kind==BuildingKind.Farm?4:2;
    public bool Utility=>Kind is BuildingKind.WaterIntake or BuildingKind.WaterTreatment or BuildingKind.SewageTreatment or BuildingKind.WindPower;
}
public static class Catalog
{
    static Dictionary<ResourceKind,double> M(double aggregate,double concrete,double steel,double wood)=>new(){[ResourceKind.Aggregate]=aggregate,[ResourceKind.Concrete]=concrete,[ResourceKind.Steel]=steel,[ResourceKind.Wood]=wood};
    public static readonly IReadOnlyDictionary<BuildingKind,BuildingDefinition> Definitions=new Dictionary<BuildingKind,BuildingDefinition>
    {
        [BuildingKind.CityHall]=new(BuildingKind.CityHall,"Prefeitura",20,18,6000,2,0,240,M(14,12,4,8)),
        [BuildingKind.WorksDepot]=new(BuildingKind.WorksDepot,"Pátio de Obras",24,22,8000,4,150,300,M(18,16,8,6)),
        [BuildingKind.House]=new(BuildingKind.House,"Casa",12,16,40000,0,4,180,M(8,6,2,10)),
        [BuildingKind.Grocery]=new(BuildingKind.Grocery,"Mercearia",16,18,60000,4,200,240,M(12,10,4,8)),
        [BuildingKind.Farm]=new(BuildingKind.Farm,"Fazenda",36,40,100000,4,400,300,M(18,8,4,12)),
        [BuildingKind.FuelStation]=new(BuildingKind.FuelStation,"Posto",24,24,100000,4,600,300,M(18,16,10,2)),
        [BuildingKind.WaterIntake]=new(BuildingKind.WaterIntake,"Captação",14,16,7000,6,1000,240,M(10,10,8,2)),
        [BuildingKind.WaterTreatment]=new(BuildingKind.WaterTreatment,"Água potável",24,26,12000,6,960,360,M(20,20,12,4)),
        [BuildingKind.SewageTreatment]=new(BuildingKind.SewageTreatment,"Esgoto",26,28,12000,6,1000,360,M(22,22,12,2)),
        [BuildingKind.WindPower]=new(BuildingKind.WindPower,"Energia eólica",18,18,14000,6,1200,360,M(16,20,20,2)),
        [BuildingKind.MechanicShop]=new(BuildingKind.MechanicShop,"Oficina mecânica",22,19,90000,4,0,300,M(16,14,8,4)),
    };
}
// Values/frequencies explicitly delegated to calibration by SPEC. They are saved with the city.
public sealed class SimulationTuning
{
    public double MapHalfSize {get;set;}=256;
    public double RoadStep {get;set;}=4;
    public double RoadWidth {get;set;}=10.5;
    public double CrosswalkSpacing {get;set;}=40;
    public double WalkingSpeed {get;set;}=1.4;
    public double VehicleSpeed {get;set;}=7;
    public double HighwaySpeed {get;set;}=12;
    public double FuelPerMetre {get;set;}=.015;
    public double FuelTank {get;set;}=40;
    public double SimulationSecondsPerRealSecond {get;set;}=1;
    public double[] SpeedMultipliers {get;set;}=[0,1,30,1000];
    public double EconomyInterval {get;set;}=1800;
    public double PurchaseFoodPerPerson {get;set;}=4;
    public double HungerDamageAfterHours {get;set;}=30;
    public double WaterClosureHours {get;set;}=72;
    public double UtilityRecoveryPerHour {get;set;}=.12;
    public double PublicMaintenanceIntervalHours {get;set;}=168;
    public decimal TotalMoney {get;set;}=20_000_000;
    public decimal CityInitialMoney {get;set;}=300_000;
    public decimal MigrantCapitalMinimum {get;set;}=45000;
    public decimal MigrantCapitalMaximum {get;set;}=160000;
    public decimal CompanyCapitalMinimum {get;set;}=250000;
    public decimal CompanyCapitalMaximum {get;set;}=450000;
    public decimal HourlySalary {get;set;}=2.5m;
    public decimal ExternalCrewHourlyPrice {get;set;}=180;
    public decimal RoadDemolitionCostPerSquareMetre {get;set;}=1.5m;
    // Short assisted vacating of an occupied residence ordered demolished (SPEC construcao: calibration).
    public double AssistedVacatingDays {get;set;}=1;
    public decimal FreightBase {get;set;}=24;
    public decimal FreightPerMetre {get;set;}=.06m;
    public decimal TruckPrice {get;set;}=3500;
    public decimal WaterUnitPrice {get;set;}=.04m;
    public decimal ElectricityUnitPrice {get;set;}=.03m;
    public decimal LoanMonthlyInterest {get;set;}=.003m;
    public decimal LoanLimit {get;set;}=100000;
    public int LoanMonths {get;set;}=12;
    public int ExternalTruckCount {get;set;}=6;
    public int ExternalPavingTeams {get;set;}=1;
    public decimal PavingMachineHire {get;set;}=90;
    public decimal PavingMachineHourlyPrice {get;set;}=80;
    public int ExternalFamilyCandidates {get;set;}=160;
    public int ExternalCompanyCandidates {get;set;}=24;
    public decimal CarPrice {get;set;}=9000;
    public decimal CarPurchaseReserve {get;set;}=6000;
    public double CarFuelPerMetre {get;set;}=.006;
    public double CarMinimumTripMetres {get;set;}=120;
    public double OutingChancePerMinute {get;set;}=.025;
    public double BypassWaitSeconds {get;set;}=10;
    public double UTurnWaitSeconds {get;set;}=180;
    public double TowMaxWaitSeconds {get;set;}=3600;
    public decimal TowServiceBase {get;set;}=60;
    public double TowLoadSeconds {get;set;}=90;
    public double TowUnloadSeconds {get;set;}=60;
    public double TowRetrySeconds {get;set;}=60;
    // Condition wear and breakdown hazard: the condition falls with the distance
    // driven; the chance per kilometre grows from the new rate to the worn one.
    public double ConditionWearPerMetre {get;set;}=1.0/60000;
    public double BreakdownChancePerKmNew {get;set;}=.002;
    public double BreakdownChancePerKmWorn {get;set;}=.08;
    public double RevisionThreshold {get;set;}=.5;
    public double LocalRepairSeconds {get;set;}=3600;
    public double LocalRevisionSeconds {get;set;}=1800;
    public double ExteriorRepairSeconds {get;set;}=4*3600;
    public double ExteriorRevisionSeconds {get;set;}=2*3600;
    public decimal MechanicHourlyPrice {get;set;}=120;
    public decimal ExteriorServiceHourlyPrice {get;set;}=150;
    public decimal LocalTowBase {get;set;}=35;
    public decimal TowTruckPrice {get;set;}=6000;
    public double ShopPushSpeed {get;set;}=1;
    public decimal ShoppingTripCostPerMetre {get;set;}=.004m;
    public Dictionary<ResourceKind,decimal> ImportPrices {get;set;}=new(){[ResourceKind.Aggregate]=14,[ResourceKind.Concrete]=25,[ResourceKind.Steel]=50,[ResourceKind.Wood]=24,[ResourceKind.Asphalt]=22,[ResourceKind.Food]=2,[ResourceKind.Fuel]=5};
}
public sealed class Building
{
    public int Id {get;set;}
    public BuildingKind Kind {get;set;}
    public Point Position {get;set;}
    public double Rotation {get;set;}
    public int ProjectId {get;set;}
    public bool Completed {get;set;}
    public bool Removed {get;set;}
    public string? OwnerAccount {get;set;}
    public int? CompanyId {get;set;}
    public int? FamilyId {get;set;}
    public ResourceStock Stock {get;set;}=new();
    public long FoodProducedUnits {get;set;}
    [JsonIgnore] public double FoodProduced {get=>FixedPoint.Value(FoodProducedUnits,FixedPoint.ResourceScale);set=>FoodProducedUnits=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    public void AddFoodProduced(double delta)=>FoodProducedUnits=FixedPoint.Add(FoodProducedUnits,delta,FixedPoint.ResourceScale);
    public double WaterRatio {get;set;}
    public double ElectricityRatio {get;set;}
    public double WaterDemand {get;set;}
    public double ElectricityDemand {get;set;}
    public double SewageProduced {get;set;}
    public double UntreatedSewage {get;set;}
    public double OperationalCapacity {get;set;}
    public long WaterOutageSecondsUnits {get;set;}
    [JsonIgnore] public double WaterOutageSeconds {get=>FixedPoint.Value(WaterOutageSecondsUnits,FixedPoint.TimeScale);set=>WaterOutageSecondsUnits=FixedPoint.Quantize(value,FixedPoint.TimeScale);}
    public void AddWaterOutageSeconds(double delta)=>WaterOutageSecondsUnits=FixedPoint.Add(WaterOutageSecondsUnits,delta,FixedPoint.TimeScale);
    public long MaintenanceRatioUnits {get;set;}=1*FixedPoint.PointScale;
    [JsonIgnore] public double MaintenanceRatio {get=>FixedPoint.Value(MaintenanceRatioUnits,FixedPoint.PointScale);set=>MaintenanceRatioUnits=FixedPoint.Quantize(value,FixedPoint.PointScale);}
    public void AddMaintenanceRatio(double delta)=>MaintenanceRatioUnits=FixedPoint.Add(MaintenanceRatioUnits,delta,FixedPoint.PointScale);
    public double MaintenanceDueAt {get;set;}
    public decimal InventoryUnitCost {get;set;}=5;
    public decimal SalePrice {get;set;}=7;
    public decimal SalaryOffer {get;set;}=2.5m;
    public double LastCustomerAt {get;set;}
    public string Cause {get;set;}="Em obra";
    public long WaterConsumedTodayUnits {get;set;}
    [JsonIgnore] public double WaterConsumedToday {get=>FixedPoint.Value(WaterConsumedTodayUnits,FixedPoint.ResourceScale);set=>WaterConsumedTodayUnits=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    public void AddWaterConsumedToday(double delta)=>WaterConsumedTodayUnits=FixedPoint.Add(WaterConsumedTodayUnits,delta,FixedPoint.ResourceScale);
    public long ElectricityConsumedTodayUnits {get;set;}
    [JsonIgnore] public double ElectricityConsumedToday {get=>FixedPoint.Value(ElectricityConsumedTodayUnits,FixedPoint.ResourceScale);set=>ElectricityConsumedTodayUnits=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    public void AddElectricityConsumedToday(double delta)=>ElectricityConsumedTodayUnits=FixedPoint.Add(ElectricityConsumedTodayUnits,delta,FixedPoint.ResourceScale);
    public decimal PaidMaintenance {get;set;}
    public bool OperationClosed {get;set;}
    public long FoodSoldTodayUnits {get;set;}
    [JsonIgnore] public double FoodSoldToday {get=>FixedPoint.Value(FoodSoldTodayUnits,FixedPoint.ResourceScale);set=>FoodSoldTodayUnits=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    public void AddFoodSoldToday(double delta)=>FoodSoldTodayUnits=FixedPoint.Add(FoodSoldTodayUnits,delta,FixedPoint.ResourceScale);
    public long FoodDemandEstimateUnits {get;set;}
    [JsonIgnore] public double FoodDemandEstimate {get=>FixedPoint.Value(FoodDemandEstimateUnits,FixedPoint.ResourceScale);set=>FoodDemandEstimateUnits=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)]
    public BuildingDemolitionOrder? Demolition {get;set;}
    [JsonIgnore] public BuildingDefinition Definition=>Catalog.Definitions[Kind];
    [JsonIgnore] public Point Door=>Position+new Point(-Math.Sin(Rotation)*Definition.Depth/2,-Math.Cos(Rotation)*Definition.Depth/2);
    public double Quantity(ResourceKind k)=>Stock.GetValueOrDefault(k);
    public void AddStock(ResourceKind k,double amount)=>Stock.Units[k]=Math.Max(0,FixedPoint.Add(Stock.Units.GetValueOrDefault(k),amount,FixedPoint.ResourceScale));
    public void AddStockUnits(ResourceKind k,long units)=>Stock.Units[k]=Math.Max(0,checked(Stock.Units.GetValueOrDefault(k)+units));
}
// A confirmed demolition: removal cost and indemnity were paid once at
// confirmation; the structure stays only while its residents vacate.
public sealed class BuildingDemolitionOrder
{
    public DateTime OrderedAt {get;set;}
    public double DeadlineSeconds {get;set;}
    public decimal RemovalCostPaid {get;set;}
    public decimal CompensationPaid {get;set;}
    public string? Beneficiary {get;set;}
    public string BeneficiaryName {get;set;}="";
    public int FamiliesAtOrder {get;set;}
    public int FamiliesRelocated {get;set;}
}
public sealed class RoadNode
{
    public int Id {get;set;}
    public bool ConstructionOnly {get;set;}
    public int? WorkProjectId {get;set;}
    public Point Position {get;set;}
    public bool Highway {get;set;}
    public List<int> Neighbours {get;set;}=[];
    public JunctionControl Control {get;set;}=JunctionControl.RightHand;
    public bool PlayerEdited {get;set;}
    public bool Crosswalk {get;set;}
    public double MaintenanceDueAt {get;set;}
    public double ElectricityRatio {get;set;}
    public long MaintenanceRatioUnits {get;set;}=1*FixedPoint.PointScale;
    [JsonIgnore] public double MaintenanceRatio {get=>FixedPoint.Value(MaintenanceRatioUnits,FixedPoint.PointScale);set=>MaintenanceRatioUnits=FixedPoint.Quantize(value,FixedPoint.PointScale);}
    public void AddMaintenanceRatio(double delta)=>MaintenanceRatioUnits=FixedPoint.Add(MaintenanceRatioUnits,delta,FixedPoint.PointScale);
}
public sealed class WorkProject
{
    public int Id {get;set;}
    public int PavingVersion {get;set;}
    public int? BuildingId {get;set;}
    public List<Point> RoadPoints {get;set;}=[];
    public List<int> BuiltRoadNodes {get;set;}=[];
    public bool IsMaintenance {get;set;}
    public DateTime? CompletedAt {get;set;}
    public List<int> MaintenanceRoadIds {get;set;}=[];
    public List<int> MaintainedRoadIds {get;set;}=[];
    public Dictionary<ResourceKind,double> Required {get;set;}=new();
    public ResourceStock Delivered {get;set;}=new();
    public ResourceStock LocalReserved {get;set;}=new();
    public WorkStatus Status {get;set;}
    public long ProgressUnits {get;set;}
    [JsonIgnore] public double Progress {get=>FixedPoint.Value(ProgressUnits,FixedPoint.PointScale);set=>ProgressUnits=FixedPoint.Quantize(value,FixedPoint.PointScale);}
    public void AddProgress(double delta)=>ProgressUnits=FixedPoint.Add(ProgressUnits,delta,FixedPoint.PointScale);
    public double WorkSeconds {get;set;}
    public decimal BudgetRemaining {get;set;}
    public decimal CostPaid {get;set;}
    public decimal ExternalCrewFee {get;set;}
    public int? CrewVehicleId {get;set;}
    public decimal CrewCostDue {get;set;}
    public int? PaverId {get;set;}
    public int? RollerId {get;set;}
    public List<Point> AppliedRoadPoints {get;set;}=[];
    public List<Point> RolledRoadPoints {get;set;}=[];
    public decimal MachineryQuoted {get;set;}
    public decimal MachineryPaid {get;set;}
    public List<int> LocalCrew {get;set;}=[];
    public bool ImportsOrdered {get;set;}
    public string Cause {get;set;}="Aguardando acesso";
    public Point Position {get;set;}
}
public sealed class Citizen
{
    public int Id {get;set;}
    public string Name {get;set;}="";
    public int FamilyId {get;set;}
    public int Age {get;set;}
    public int? WorkplaceId {get;set;}
    public int? ConstructionProjectId {get;set;}
    public Point Position {get;set;}
    public bool InsideMap {get;set;}
    public bool Resident {get;set;}
    public bool Alive {get;set;}=true;
    public int? VehicleId {get;set;}
    public int? BoardingVehicleId {get;set;}
    public List<Point> Path {get;set;}=[];
    public int PathIndex {get;set;}
    public JourneyPurpose Purpose {get;set;}
    public int? DestinationId {get;set;}
    public long HungerMicroseconds {get;set;}
    [JsonIgnore] public double HungerHours {get=>HungerMicroseconds/(3600.0*FixedPoint.TimeScale);set=>HungerMicroseconds=FixedPoint.Quantize(value*3600,FixedPoint.TimeScale);}
    public void AddHungerSeconds(double seconds)=>HungerMicroseconds=FixedPoint.Add(HungerMicroseconds,seconds,FixedPoint.TimeScale);
    public long HealthUnits {get;set;}=100*FixedPoint.PointScale;
    [JsonIgnore] public double Health {get=>FixedPoint.Value(HealthUnits,FixedPoint.PointScale);set=>HealthUnits=FixedPoint.Quantize(value,FixedPoint.PointScale);}
    public void AddHealth(double delta)=>HealthUnits=FixedPoint.Add(HealthUnits,delta,FixedPoint.PointScale);
    public double Satisfaction {get;set;}=65;
    public long BagFoodUnits {get;set;}
    [JsonIgnore] public double BagFood {get=>FixedPoint.Value(BagFoodUnits,FixedPoint.ResourceScale);set=>BagFoodUnits=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    public void AddBagFood(double delta)=>BagFoodUnits=FixedPoint.Add(BagFoodUnits,delta,FixedPoint.ResourceScale);
    public long WorkSecondsTodayUnits {get;set;}
    [JsonIgnore] public double WorkSecondsToday {get=>FixedPoint.Value(WorkSecondsTodayUnits,FixedPoint.TimeScale);set=>WorkSecondsTodayUnits=FixedPoint.Quantize(value,FixedPoint.TimeScale);}
    public void AddWorkSecondsToday(double delta)=>WorkSecondsTodayUnits=FixedPoint.Add(WorkSecondsTodayUnits,delta,FixedPoint.TimeScale);
    public decimal WagesAccrued {get;set;}
    public double LastPaidAt {get;set;}
    public double NextDecisionAt {get;set;}
    public int PathRoadRevision {get;set;}
    public int? CrossingNodeId {get;set;}
    public List<int> TriedShops {get;set;}=[];
    public int WeeklyDayOff {get;set;}
    public int Shift {get;set;}
    public string Activity {get;set;}="Chegando";
    public string Decision {get;set;}="";
    public Pr57PersonalHistory History {get;set;}=new();
    public double OutingUntil {get;set;}
    [JsonIgnore] public string Account=>"sim:"+Id;
    [JsonIgnore] public bool Moving=>PathIndex<Path.Count;
}
public sealed class Family
{
    public int Id {get;set;}
    public List<int> Members {get;set;}=[];
    public int? HomeId {get;set;}
    public long PantryUnits {get;set;}
    [JsonIgnore] public double Pantry {get=>FixedPoint.Value(PantryUnits,FixedPoint.ResourceScale);set=>PantryUnits=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    public void AddPantry(double delta)=>PantryUnits=FixedPoint.Add(PantryUnits,delta,FixedPoint.ResourceScale);
    public double LastMigrationReview {get;set;}
    public bool Emigrating {get;set;}
}
public sealed class Company
{
    public int Id {get;set;}
    public string Name {get;set;}="";
    public List<int> Buildings {get;set;}=[];
    public bool Active {get;set;}=true;
    public double LossStartedAt {get;set;}
    public string Decision {get;set;}="";
    [JsonIgnore] public string Account=>"company:"+Id;
}
// A local permission, not a shortened physical route. Snapshots identify the
// actual path; a reroute cannot reuse the same numeric endpoint accidentally.
public sealed class LocalMovementAdmission
{
    public List<int> Route {get;set;}=[];
    public List<Point> SitePath {get;set;}=[];
    public int EndRouteIndex {get;set;}
    public bool ReachesDock {get;set;}
}

public sealed class MovementPose
{
    public Point Position {get;set;}
    public double Heading {get;set;}
}

public sealed class Vehicle
{
    public int Id {get;set;}
    public TruckBody Body {get;set;}
    public bool ConstructionAccess {get;set;}
    public PavingMachine Machine {get;set;}
    public MachinePhase MachinePhase {get;set;}
    public int? MachineProjectId {get;set;}
    public List<Point> SitePath {get;set;}=[];
    public int SitePathIndex {get;set;}
    public Point? WorkTarget {get;set;}
    public bool FeedingReady {get;set;}
    public bool SitePathBeforeRoad {get;set;}
    public int? SiteProjectId {get;set;}
    public double? SiteDockHeading {get;set;}
    public string OwnerAccount {get;set;}="global";
    public bool External {get;set;}
    public Point Position {get;set;}
    public double Heading {get;set;}
    public long FuelUnits {get;set;}
    [JsonIgnore] public double Fuel {get=>FixedPoint.Value(FuelUnits,FixedPoint.ResourceScale);set=>FuelUnits=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    public void AddFuel(double delta)=>FuelUnits=FixedPoint.Add(FuelUnits,delta,FixedPoint.ResourceScale);
    public long FuelBurnedLastStepUnits {get;set;}
    [JsonIgnore] public double FuelBurnedLastStep {get=>FixedPoint.Value(FuelBurnedLastStepUnits,FixedPoint.ResourceScale);set=>FuelBurnedLastStepUnits=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    public void AddFuelBurnedLastStep(double delta)=>FuelBurnedLastStepUnits=FixedPoint.Add(FuelBurnedLastStepUnits,delta,FixedPoint.ResourceScale);
    public VehicleStatus Status {get;set;}
    public List<int> Route {get;set;}=[];
    public int RouteIndex {get;set;}
    public Point? DockPosition {get;set;}
    public Point? HoldingPosition {get;set;}
    public int? DriverId {get;set;}
    public string? ExternalDriverId {get;set;}
    public int? ShipmentId {get;set;}
    public int? HomeBuildingId {get;set;}
    public int? DestinationBuildingId {get;set;}
    public int? RefuellingAt {get;set;}
    public List<int> SavedRoute {get;set;}=[];
    public ResourceStock Cargo {get;set;}=new();
    public double CargoCapacity {get;set;}=150;
    public long LoadTimerUnits {get;set;}
    [JsonIgnore] public double LoadTimer {get=>FixedPoint.Value(LoadTimerUnits,FixedPoint.TimeScale);set=>LoadTimerUnits=FixedPoint.Quantize(value,FixedPoint.TimeScale);}
    public void AddLoadTimer(double delta)=>LoadTimerUnits=FixedPoint.Add(LoadTimerUnits,delta,FixedPoint.TimeScale);
    public string Cause {get;set;}="No exterior";
    public double WaitingSince {get;set;}
    public int? IntersectionClaim {get;set;}
    public double? IntersectionEnteredAt {get;set;}
    // ARCHITECTURE "Espaços compartilhados": an admitted local manoeuvre — the
    // dock whose corridor it owns (entering, at or leaving it) and the junctions
    // and bends it holds on the way, until its body has left them.
    public Point? ManoeuvreDock {get;set;}
    // Physical dock-to-lane sweep, retained even after its route prefix is passed.
    public List<MovementPose> DockCorridor {get;set;}=[];
    // null in older saves: a return route at the dock is not an admitted exit.
    public bool? ManoeuvreDeparture {get;set;}
    public LocalMovementAdmission? MovementAdmission {get;set;}
    public List<int> ManoeuvreSpaces {get;set;}=[];
    public bool CrewAboard {get;set;}
    public int CrewCount {get;set;}
    public bool PassengerCar {get;set;}
    public string? ParkingSpot {get;set;}
    public List<int> Passengers {get;set;}=[];
    public JourneyPurpose TripPurpose {get;set;}
    // Physical bypass of a vehicle stopped on the road: remaining waypoints and
    // the road node to resume at once back in the lane (a node, not an index:
    // the route may be replanned meanwhile).
    public List<Point> Detour {get;set;}=[];
    public int DetourResumeNode {get;set;}
    // Tow service: on the tow truck, its phase, the vehicle served and the
    // station it delivers to; on the stranded vehicle, the assigned tow, the
    // road node its trip resumes towards and when the call is retried (after a
    // pause, or once the road network changes after an impossible call).
    public TowPhase TowPhase {get;set;}
    public int? TowTargetId {get;set;}
    public TowDestination TowDestination {get;set;}
    public int? TowStationId {get;set;}
    public int? TowTruckId {get;set;}
    public int? TowResumeNode {get;set;}
    public double TowCalledAt {get;set;}
    public double TowRetryAt {get;set;}
    public int? TowRoadRevision {get;set;}
    public bool TowPrepaid {get;set;}
    // A loaded tow waiting for the lane beside a queue that faces it: since when,
    // and whether it found no other way out (then that queue turns back).
    public double TowExitWaitSince {get;set;}
    public bool TowExitNoOther {get;set;}
    // Condition (0..1) falls with the distance driven; a worn vehicle in use may
    // break down. Presentation contract: Condition, BrokenDown, ServiceShopId and
    // ServiceBay (0 = lift, 1 = floor) while on a bay of a local mechanic shop.
    public long ConditionUnits {get;set;}=1*FixedPoint.PointScale;
    [JsonIgnore] public double Condition {get=>FixedPoint.Value(ConditionUnits,FixedPoint.PointScale);set=>ConditionUnits=FixedPoint.Quantize(Math.Clamp(value,0,1),FixedPoint.PointScale);}
    public bool BrokenDown {get;set;}
    public ServiceKind Service {get;set;}
    public ServicePhase ServicePhase {get;set;}
    public int? ServiceShopId {get;set;}
    public int? ServiceBay {get;set;}
    public int? ServiceSpot {get;set;}
    public long ServiceWorkUnits {get;set;}
    [JsonIgnore] public double ServiceWork {get=>FixedPoint.Value(ServiceWorkUnits,FixedPoint.TimeScale);set=>ServiceWorkUnits=FixedPoint.Quantize(value,FixedPoint.TimeScale);}
    public double ServiceSince {get;set;}
    public double ServiceUntil {get;set;}
    // A short manoeuvre inside a lot at walking pace: pushed into or out of a bay,
    // or set down at the owner's space.
    public List<Point> PushPath {get;set;}=[];
    public double? PushHeading {get;set;}
}
public sealed class Shipment
{
    public int Id {get;set;}
    public int? ReceivingMachineId {get;set;}
    public ShipmentPurpose Purpose {get;set;}
    public int? ProjectId {get;set;}
    public int? OriginalProjectId {get;set;}
    public int? SourceBuildingId {get;set;}
    public int? DestinationBuildingId {get;set;}
    public int VehicleId {get;set;}
    public bool Complete {get;set;}
    public bool Cancelled {get;set;}
    public bool PickedUp {get;set;}
    public string GoodsOwnerAccount {get;set;}="city";
    public ResourceStock Cargo {get;set;}=new();
    public decimal PricePaid {get;set;}
    public Point Destination {get;set;}
}
public enum MunicipalRelocationStage { Constructing, Transferring, Completed, Cancelled }
// Registro mínimo de uma mudança de prédio municipal concluído: liga a
// estrutura antiga à obra substituta e guarda os compromissos ainda não pagos
// (retirada e frete do estoque), que entram em Committed até serem pagos ou
// liberados. Dinheiro, estoque e pessoas continuam nos seus lugares reais.
public sealed class MunicipalRelocationOrder
{
    public int Id {get;set;}
    public BuildingKind Kind {get;set;}
    public int SourceBuildingId {get;set;}
    public int TargetBuildingId {get;set;}
    public int TargetProjectId {get;set;}
    public MunicipalRelocationStage Stage {get;set;}
    public DateTime OrderedAt {get;set;}
    public DateTime? FinishedAt {get;set;}
    public decimal RemovalReserved {get;set;}
    public decimal TransferReserved {get;set;}
    public decimal RemovalPaid {get;set;}
    public decimal TransferPaid {get;set;}
    public long StockRecoveryBlockedSecondsUnits {get;set;}
    [JsonIgnore] public double StockRecoveryBlockedSeconds {get=>FixedPoint.Value(StockRecoveryBlockedSecondsUnits,FixedPoint.TimeScale);set=>StockRecoveryBlockedSecondsUnits=FixedPoint.Quantize(value,FixedPoint.TimeScale);}
    public void AddStockRecoveryBlockedSeconds(double delta)=>StockRecoveryBlockedSecondsUnits=FixedPoint.Add(StockRecoveryBlockedSecondsUnits,delta,FixedPoint.TimeScale);
    public long LastRecoveryStockUnits {get;set;}=-FixedPoint.ResourceScale;
    [JsonIgnore] public double LastRecoveryStock {get=>FixedPoint.Value(LastRecoveryStockUnits,FixedPoint.ResourceScale);set=>LastRecoveryStockUnits=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    public List<int> TransferShipmentIds {get;set;}=[];
    public ResourceStock StockLost {get;set;}=new();
    public int WorkersContinued {get;set;}
    public int WorkersLeft {get;set;}
    public string Cause {get;set;}="";
    [JsonIgnore] public bool Active=>Stage is MunicipalRelocationStage.Constructing or MunicipalRelocationStage.Transferring;
}
public sealed record PersonalEvent(DateTime At,string Description);
public sealed record MoneyMovement(DateTime At,string From,string To,decimal Amount,string Category,string Reason,int? EntityId);
public sealed class Obligation
{
    public int Id {get;set;}
    public string Debtor {get;set;}="";
    public string Creditor {get;set;}="city";
    public decimal Outstanding {get;set;}
    public int? LoanId {get;set;}
    public decimal PrincipalOutstanding {get;set;}
    public string Kind {get;set;}="";
    public int? BuildingId {get;set;}
    public DateTime Due {get;set;}
}
public sealed class MunicipalLoan
{
    public int Id {get;set;}
    public decimal Principal {get;set;}
    public decimal MonthlyPrincipal {get;set;}
    public decimal ScheduledPrincipal {get;set;}
    public decimal Rate {get;set;}
    public DateTime NextDue {get;set;}
    public int RemainingMonths {get;set;}
}
public sealed class ExternalFamilyCandidate
{
    public int Id {get;set;}
    public int Adults {get;set;}
    public decimal Capital {get;set;}
    public double RiskTolerance {get;set;}
    public bool Migrated {get;set;}
    public string Cause {get;set;}="Aguardando oportunidade";
}
public sealed class ExternalCompanyCandidate
{
    public int Id {get;set;}
    public decimal Capital {get;set;}
    public bool Entered {get;set;}
    public double RiskTolerance {get;set;}
    public string Cause {get;set;}="Aguardando oportunidade";
}
public sealed class CityState
{
    public int SchemaVersion {get;set;}=1;
    public int QuantityStateVersion {get;set;}
    public int ConstructionStateVersion {get;set;}
    // 1: vehicle condition, breakdowns and mechanic service. Older saves load with
    // every vehicle at full condition and no service in progress.
    public int VehicleStateVersion {get;set;}
    public int Seed {get;set;}
    public string Name {get;set;}="";
    public SimulationTuning Tuning {get;set;}=new();
    public ulong RandomState {get;set;}
    // Separate stream for vehicle breakdowns, so the other random decisions keep their sequence.
    public ulong BreakdownRandomState {get;set;}
    public int NextId {get;set;}=1;
    public DateTime Now {get;set;}=new(2026,1,1,6,0,0);
    public long ElapsedSecondsUnits {get;set;}
    [JsonIgnore] public double ElapsedSeconds {get=>FixedPoint.Value(ElapsedSecondsUnits,FixedPoint.TimeScale);set=>ElapsedSecondsUnits=FixedPoint.Quantize(value,FixedPoint.TimeScale);}
    public void AddElapsedSeconds(double delta)=>ElapsedSecondsUnits=FixedPoint.Add(ElapsedSecondsUnits,delta,FixedPoint.TimeScale);
    public double EconomyNextAt {get;set;}
    public DateTime NextDaily {get;set;}
    public DateTime NextTaxDate {get;set;}
    public Point Entry {get;set;}
    public Point LakeCentre {get;set;}
    public double LakeRadius {get;set;}
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)]
    public List<Point>? LakeBoundary {get;set;}
    public double LakePollution {get;set;}
    public Dictionary<long,decimal> PollutionCells {get;set;}=new();
    public bool CityHallPlaced {get;set;}
    public int RoadRevision {get;set;}
    public Dictionary<string,decimal> Wallets {get;set;}=new();
    public List<MoneyMovement> Ledger {get;set;}=[];
    public List<Obligation> Obligations {get;set;}=[];
    public List<MunicipalLoan> Loans {get;set;}=[];
    public List<Building> Buildings {get;set;}=[];
    public List<RoadNode> Roads {get;set;}=[];
    public List<WorkProject> Projects {get;set;}=[];
    public List<Citizen> Citizens {get;set;}=[];
    public List<Family> Families {get;set;}=[];
    public List<Company> Companies {get;set;}=[];
    public List<Vehicle> Vehicles {get;set;}=[];
    public bool PavingFleetInitialized {get;set;}
    public List<Shipment> Shipments {get;set;}=[];
    public List<MunicipalRelocationOrder> MunicipalRelocations {get;set;}=[];
    public List<ExternalFamilyCandidate> FamilyCandidates {get;set;}=[];
    public List<ExternalCompanyCandidate> CompanyCandidates {get;set;}=[];
    public ResourceStock OutsideStock {get;set;}=new();
    public decimal[] TaxRates {get;set;}=[.01m,.01m,.01m,.01m,.01m,.01m];
    public double PotableCapacity {get;set;}
    public double WaterDemand {get;set;}
    public double ElectricityCapacity {get;set;}
    public double ElectricityDemand {get;set;}
    public double SewageCapacity {get;set;}
    public double SewageDemand {get;set;}
    public double UntreatedSewage {get;set;}
    public double WindFactor {get;set;}
    public double RawWaterCapacity {get;set;}
    public long StreetElectricityConsumedTodayUnits {get;set;}
    [JsonIgnore] public double StreetElectricityConsumedToday {get=>FixedPoint.Value(StreetElectricityConsumedTodayUnits,FixedPoint.ResourceScale);set=>StreetElectricityConsumedTodayUnits=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    public void AddStreetElectricityConsumedToday(double delta)=>StreetElectricityConsumedTodayUnits=FixedPoint.Add(StreetElectricityConsumedTodayUnits,delta,FixedPoint.ResourceScale);
    public decimal WaterWageCostToday {get;set;}
    public decimal ElectricityWageCostToday {get;set;}
    public long WaterAvailableTodayUnits {get;set;}
    [JsonIgnore] public double WaterAvailableToday {get=>FixedPoint.Value(WaterAvailableTodayUnits,FixedPoint.ResourceScale);set=>WaterAvailableTodayUnits=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    public void AddWaterAvailableToday(double delta)=>WaterAvailableTodayUnits=FixedPoint.Add(WaterAvailableTodayUnits,delta,FixedPoint.ResourceScale);
    public long ElectricityAvailableTodayUnits {get;set;}
    [JsonIgnore] public double ElectricityAvailableToday {get=>FixedPoint.Value(ElectricityAvailableTodayUnits,FixedPoint.ResourceScale);set=>ElectricityAvailableTodayUnits=FixedPoint.Quantize(value,FixedPoint.ResourceScale);}
    public void AddElectricityAvailableToday(double delta)=>ElectricityAvailableTodayUnits=FixedPoint.Add(ElectricityAvailableTodayUnits,delta,FixedPoint.ResourceScale);
    public decimal WaterUnitPrice {get;set;}=.04m;
    public decimal ElectricityUnitPrice {get;set;}=.03m;
    public double MaintenanceNextAt {get;set;}
    public string LastMessage {get;set;}="Comece por uma rua ou posicione a Prefeitura";
    public List<PersonalEvent> CityEvents {get;set;}=[];
}
public sealed record CommandResult(bool Success,string Message,int? Id=null);
public sealed record PlacementQuote(bool PhysicallyValid,bool Accessible,decimal Total,Dictionary<ResourceKind,double> Materials,string Message)
{
    public decimal MachineryEstimate {get;init;}
}
