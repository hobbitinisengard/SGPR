using UnityEngine;

/// <summary>
/// A (P)roportional, (I)ntegral, (D)erivative Controller
/// </summary>
/// <remarks>
/// The controller should be able to control any process with a
/// measureable value, a known ideal value and an input to the
/// process that will affect the measured value.
/// </remarks>
/// <see cref="https://en.wikipedia.org/wiki/PID_controller"/>
public class PidController : MonoBehaviour
{
	/// <summary> The desired value </summary>
	public float SetPoint = 0;
	/// <summary> The proportional term produces an output value that is proportional to the current error value.  </summary>
	public float GainProportional = 0;
	/// <summary> The integral term is proportional to both the magnitude of the error and the duration of the error </summary>
	public float GainIntegral = 0;
	/// <summary>The derivative term is proportional to the rate of change of the error </summary>
	public float GainDerivative = 0;
	
	const float OutputMax = 1;
	const float OutputMin = -1;
	/// <summary> Adjustment made by considering the accumulated error over time  </summary>
	/// <remarks> An alternative formulation of the integral action, is the proportional-summation-difference used in discrete-time systems </remarks>
	float integralTerm = 0;
	float processVariable = 0;

	/// <summary>
	/// The controller output
	/// </summary>
	/// <param name="timeDelta">timespan of the elapsed time
	/// since the previous time that ControlVariable was called</param>
	/// <returns>Value of the variable that needs to be controlled</returns>
	public float Step(float timeDelta)
	{
		float error = SetPoint - ProcessVariable;

		// integral term calculation
		integralTerm += (GainIntegral * error * timeDelta);
		integralTerm = Mathf.Clamp(integralTerm, OutputMin, OutputMax);

		// derivative term calculation
		float dInput = processVariable - ProcessVariableLast;
		float derivativeTerm = GainDerivative * (dInput / timeDelta);

		// proportional term calcullation
		float proportionalTerm = GainProportional * error;

		float output = proportionalTerm + integralTerm - derivativeTerm;

		output = Mathf.Clamp(output, OutputMin, OutputMax);

		return output;
	}
	/// <summary>
	/// The current value
	/// </summary>
	public float ProcessVariable
	{
		get { return processVariable; }
		set
		{
			ProcessVariableLast = processVariable;
			processVariable = value;
		}
	}

	/// <summary>
	/// The last reported value (used to calculate the rate of change)
	/// </summary>
	float ProcessVariableLast = 0;

	
}
