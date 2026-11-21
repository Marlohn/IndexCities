namespace IndexCities.Simulation;

// PR57 #309: experiment-only publication at the nested ResumeFromExterior
// writer's exits. This updates ONLY the observational #306 projection.
// It does not replace any canonical reader or prove a complete write journal.
public sealed partial class CitySimulation
{
    bool _pr57309Resume = Environment.GetEnvironmentVariable("PR57_309_RESUME_PUBLISH") == "1";

    void Pr57309PublishResume(Vehicle target)
    {
        if (_pr57309Resume) Pr57307Publish(target);
    }
}
