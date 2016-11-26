using System;

namespace MechJim {
    public class PIDLoop {
        public double Kp { get; set; }
        public double Ki { get; set; }
        public double Kd { get; set; }
        public double Input { get; set; }
        public double Setpoint { get; set; }
        public double Error { get; set; }
        public double Output { get; set; }
        public double MinOutput { get; set; }
        public double MaxOutput { get; set; }
        public double ErrorSum { get; set; }
        public double PTerm { get; set; }
        public double ITerm { get; set; }
        public double DTerm { get; set; }
        public bool ExtraUnwind { get; set; }
        public double ChangeRate { get; set; }
        public bool unWinding { get; set; }

        public PIDLoop() : this(1, 0, 0) { }

        public PIDLoop(double kp, double ki, double kd, double maxoutput = double.MaxValue, double minoutput = double.MinValue, bool extraUnwind = false) {
            Kp = kp;
            Ki = ki;
            Kd = kd;
            Input = 0;
            Setpoint = 0;
            Error = 0;
            Output = 0;
            MaxOutput = maxoutput;
            MinOutput = minoutput;
            ErrorSum = 0;
            PTerm = 0;
            ITerm = 0;
            DTerm = 0;
            ExtraUnwind = extraUnwind;
        }

        public double Update(double input, double setpoint, double minOutput, double maxOutput) {
            MaxOutput = maxOutput;
            MinOutput = minOutput;
            Setpoint = setpoint;
            return Update(input);
        }

        public double Update(double input, double setpoint, double maxOutput) {
            return Update(input, setpoint, -maxOutput, maxOutput);
        }

        public double Update(double input)
        {
            double error = Setpoint - input;
            double pTerm = error * Kp;
            double iTerm = 0;
            double dTerm = 0;
            double dt = TimeWarp.fixedDeltaTime;
            if (Ki != 0) {
                if (ExtraUnwind) {
                    if (Math.Sign(error) != Math.Sign(ErrorSum)) {
                        if (!unWinding) {
                            Ki *= 2;
                            unWinding = true;
                        }
                    } else if (unWinding) {
                        Ki /= 2;
                        unWinding = false;
                    }
                }
                iTerm = ITerm + error * dt * Ki;
            }
            ChangeRate = (input - Input) / dt;
            if (Kd != 0) {
                dTerm = -ChangeRate * Kd;
            }
            Output = pTerm + iTerm + dTerm;
            if (Output > MaxOutput) {
                Output = MaxOutput;
                if (Ki != 0) {
                    iTerm = Output - Math.Min(pTerm + dTerm, MaxOutput);
                }
            }
            if (Output < MinOutput) {
                Output = MinOutput;
                if (Ki != 0) {
                    iTerm = Output - Math.Max(pTerm + dTerm, MinOutput);
                }
            }
            Input = input;
            Error = error;
            PTerm = pTerm;
            ITerm = iTerm;
            DTerm = dTerm;
            if (Ki != 0)
                ErrorSum = iTerm / Ki;
            else
                ErrorSum = 0;
            return Output;
        }

        public void ResetI() {
            ErrorSum = 0;
            ITerm = 0;
        }
    }
}
