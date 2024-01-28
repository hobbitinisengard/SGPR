// Using Remi Coulom's K1999 Path-Optimisation Algorithm to calculate
// racing line.
//This is an adaption of Remi Coulom's K1999 driver for TORCS
using System.Collections.Generic;
using UnityEngine;
public class K1999
{
	float[] tx, ty;
	float[] txRight, tyRight;
	float[] txLeft, tyLeft;
	float[] tLane;
	float[] tRInverse;
	int Divs = 0;
	readonly RacingPathParams data;
	//readonly float SecurityR = 2f;     // Security radius
	//readonly float SideDistExt = 1f;  // Security distance wrt outside
	//readonly float SideDistInt = 1f;  // Security distance wrt inside
	//readonly int Iterations = 500;    // Number of smoothing operations
	public K1999(RacingPathParams data)
	{
		this.data = data;
	}
	//public K1999(float SecurityR, float SideDistExt, float SideDistInt, int iterations)
	//{
	//	this.SecurityR = SecurityR;
	//	this.SideDistExt = SideDistExt;
	//	this.SideDistInt = SideDistInt;
	//	this.Iterations = iterations;
	//}
	void UpdateTxTy(int i)
	{
		tx[i] = tLane[i] * txRight[i] + (1 - tLane[i]) * txLeft[i];
		ty[i] = tLane[i] * tyRight[i] + (1 - tLane[i]) * tyLeft[i];
	}

	//void K1999::DrawPath(std::ostream &out)
	//{
	//	for (int i = 0; i <= Divs; i++)
	//	{
	//		int j = i % Divs;
	//		out << txLeft[j] << ' ' << tyLeft[j] << ' ';
	//		out << tx[j] << ' ' << ty[j] << ' ';
	//		out << txRight[j] << ' ' << tyRight[j] << ' ';
	//		out << tLane[j] << ' ' << tRInverse[j] << '\n';
	//	}
	//	out << '\n';
	//}

	// Compute the inverse of the radius
	float GetRInverse(int prev, float x, float y, int next)
	{
		float x1 = tx[next] - x;
		float y1 = ty[next] - y;
		float x2 = tx[prev] - x;
		float y2 = ty[prev] - y;
		float x3 = tx[next] - tx[prev];
		float y3 = ty[next] - ty[prev];

		float det = x1 * y2 - x2 * y1;
		float n1 = x1 * x1 + y1 * y1;
		float n2 = x2 * x2 + y2 * y2;
		float n3 = x3 * x3 + y3 * y3;
		float nnn = Mathf.Sqrt(n1 * n2 * n3);

		float c = 2 * det / nnn;
		return c;
	}
	float Mag(float x, float y)
	{
		return Mathf.Sqrt(x * x + y * y);
	}

	// Change lane value to reach a given radius
	void AdjustRadius(int prev, int i, int next, float TargetRInverse, float Security = 0)
	{
		float OldLane = tLane[i];

		float Width = Mag((txLeft[i] - txRight[i]), (tyLeft[i] - tyRight[i]));

		// Start by aligning points for a reasonable initial lane
		tLane[i] = (-(ty[next] - ty[prev]) * (txLeft[i] - tx[prev]) +
				(tx[next] - tx[prev]) * (tyLeft[i] - ty[prev])) /
				((ty[next] - ty[prev]) * (txRight[i] - txLeft[i]) -
				(tx[next] - tx[prev]) * (tyRight[i] - tyLeft[i]));

		// the original algorithm allows going outside the track
		/*
		if (tLane[i] < -0.2)
			tLane[i] = -0.2;
		else if (tLane[i] > 1.2)
			tLane[i] = 1.2;*/
		if (tLane[i] < 0.2f)
			tLane[i] = 0.2f;
		else if (tLane[i] > 0.8f)
			tLane[i] = 0.8f;

		UpdateTxTy(i);

		//
		// Newton-like resolution method
		//
		const float dLane = 0.0001f;

		float dx = dLane * (txRight[i] - txLeft[i]);
		float dy = dLane * (tyRight[i] - tyLeft[i]);

		float dRInverse = GetRInverse(prev, tx[i] + dx, ty[i] + dy, next);

		if (dRInverse > 0.000000001)
		{
			tLane[i] += (dLane / dRInverse) * TargetRInverse;

			float ExtLane = (data.SideDistExt + Security) / Width;
			float IntLane = (data.SideDistInt + Security) / Width;
			if (ExtLane > 0.5)
				ExtLane = 0.5f;
			if (IntLane > 0.5)
				IntLane = 0.5f;

			if (TargetRInverse >= 0.0)
			{
				if (tLane[i] < IntLane)
					tLane[i] = IntLane;
				if (1 - tLane[i] < ExtLane)
				{
					if (1 - OldLane < ExtLane)
						tLane[i] = Mathf.Min(OldLane, tLane[i]);
					else
						tLane[i] = 1 - ExtLane;
				}
			}
			else
			{
				if (tLane[i] < ExtLane)
				{
					if (OldLane < ExtLane)
						tLane[i] = Mathf.Max(OldLane, tLane[i]);
					else
						tLane[i] = ExtLane;
				}
				if (1 - tLane[i] < IntLane)
					tLane[i] = 1 - IntLane;
			}
		}
		UpdateTxTy(i);
	}

	// Smooth path
	void Smooth(int Step)
	{
		int prev = ((Divs - Step) / Step) * Step;
		int prevprev = prev - Step;
		int next = Step;
		int nextnext = next + Step;

		assert(prev >= 0);
		//std::cout << Divs << ", " << Step << ", " << prev << ", " << tx.size() << std::endl;
		assert(prev < tx.Length);
		assert(prev < ty.Length);
		assert(next < tx.Length);
		assert(next < ty.Length);

		for (int i = 0; i <= Divs - Step; i += Step)
		{
			float ri0 = GetRInverse(prevprev, tx[prev], ty[prev], i);
			float ri1 = GetRInverse(i, tx[next], ty[next], nextnext);
			float lPrev = Mag(tx[i] - tx[prev], ty[i] - ty[prev]);
			float lNext = Mag(tx[i] - tx[next], ty[i] - ty[next]);

			float TargetRInverse = (lNext * ri0 + lPrev * ri1) / (lNext + lPrev);

			float Security = lPrev * lNext / (8 * data.SecurityR);
			AdjustRadius(prev, i, next, TargetRInverse, Security);

			prevprev = prev;
			prev = i;
			next = nextnext;
			nextnext = next + Step;
			if (nextnext > Divs - Step)
				nextnext = 0;
		}
	}

	// Interpolate between two control points
	void StepInterpolate(int iMin, int iMax, int Step)
	{
		int next = (iMax + Step) % Divs;
		if (next > Divs - Step)
			next = 0;

		int prev = (((Divs + iMin - Step) % Divs) / Step) * Step;
		if (prev > Divs - Step)
			prev -= Step;

		float ir0 = GetRInverse(prev, tx[iMin], ty[iMin], iMax % Divs);
		float ir1 = GetRInverse(iMin, tx[iMax % Divs], ty[iMax % Divs], next);
		for (int k = iMax; --k > iMin;)
		{
			float x = (float)(k - iMin) / (float)(iMax - iMin);
			float TargetRInverse = x * ir1 + (1 - x) * ir0;
			AdjustRadius(iMin, k, iMax % Divs, TargetRInverse);
		}
	}

	// Calls to StepInterpolate for the full path
	void Interpolate(int Step)
	{
		if (Step > 1)
		{
			int i;
			for (i = Step; i <= Divs - Step; i += Step)
				StepInterpolate(i - Step, i, Step);
			StepInterpolate(i - Step, Divs, Step);
		}
	}

	public void CalcRaceLine()
	{
		int stepsize = 6;

		//abort if the track isn't long enough
		if (tx.Length < stepsize)
			return;

		// Smoothing loop
		for (int Step = stepsize; (Step /= 2) > 0;)
		{
			for (int i = data.Iterations * ((int)Mathf.Sqrt(Step)); --i >= 0;)
				Smooth(Step);
			Interpolate(Step);
		}

		// Compute curvature along the path
		for (int i = Divs; --i >= 0;)
		{
			int next = (i + 1) % Divs;
			int prev = (i - 1 + Divs) % Divs;

			float rInverse = GetRInverse(prev, tx[i], ty[i], next);
			tRInverse[i] = rInverse;
		}

		//# ifdef DRAWPATH
		//		std::ofstream ofs("k1999.path");
		//		DrawPath(ofs);
		//#endif
	}

	private void assert(bool v)
	{
		if (!v)
			Debug.LogError("assertion failed");
	}
	public Vector4[] GetRacingLine(in List<Vector3> leftLimits, in List<Vector3> rightLimits)
	{
		if (Divs == 0)
			return null;

		Vector4[] racingLine = new Vector4[Divs];
		for (int i = 0; i < Divs; ++i)
		{
			Vector3 point = leftLimits[i] * (1.0f - tLane[i]) + rightLimits[i] * tLane[i];
			racingLine[i] = new Vector4(point.x, point.y, point.z, tRInverse[i]);
		}
		return racingLine;
	}
	public void LoadData(in List<Vector3> leftLimits, in List<Vector3> rightLimits)//const RoadStrip & road)
	{
		Divs = leftLimits.Count;
		tx = new float[Divs];
		ty = new float[Divs];
		tRInverse = new float[Divs];
		txLeft = new float[Divs];
		tyLeft = new float[Divs];
		txRight = new float[Divs];
		tyRight = new float[Divs];
		tLane = new float[Divs];

		for (int i = 0; i < leftLimits.Count; ++i)
		{
			//txLeft.push_back(p.GetPoint(3, 0)[1]);
			//tyLeft.push_back(-p.GetPoint(3, 0)[0]);
			//txRight.push_back(p.GetPoint(3, 3)[1]);
			//tyRight.push_back(-p.GetPoint(3, 3)[0]);
			txLeft[i] = leftLimits[i].x;
			tyLeft[i] = leftLimits[i].z;
			txRight[i] = rightLimits[i].x;
			tyRight[i] = rightLimits[i].z;
			tLane[i] = 0.5f;
			tx[i] = 0;
			ty[i] = 0;
			tRInverse[i] = 0;

			UpdateTxTy(i);
		}
	}
}



