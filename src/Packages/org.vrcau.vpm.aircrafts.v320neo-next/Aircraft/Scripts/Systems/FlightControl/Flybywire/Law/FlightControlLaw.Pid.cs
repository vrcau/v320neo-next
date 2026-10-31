using UnityEngine;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.Flybywire.Law
{
    public partial class FlightControlLaw
    {
        public static float RunPid(float target,
            float current,
            ref float integrator,
            ref float lastError,
            float kp,
            float ki,
            float kd,
            float minIntegrator,
            float maxIntegrator,
            bool frozeIntegrator = false)
        {
            var error = target - current;
            var lastErrorCopy = lastError;
            lastError = error;

            if (!frozeIntegrator)
            {
                integrator = Mathf.Clamp(integrator + error * Time.fixedDeltaTime, minIntegrator, maxIntegrator);
            }

            return kp * error + ki * integrator + kd * (error - lastErrorCopy) / Time.fixedDeltaTime;
        }
    }
}