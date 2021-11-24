#include "Point.hpp"
#include "ObjectState.hpp"
#include "RealNumber.h"


using namespace Objects;

static const char*		XFieldName				= "x";
static const char*		YFieldName				= "y";
static const char*		ZFieldName				= "z";


Point3D::Point3D ()
	: X (0.0)
	, Y (0.0)
	, Z (0.0)
{
}


Point3D::Point3D (double x, double y, double z)
	: X (x)
	, Y (y)
	, Z (z)
{
}


Point3D::Point3D (const API_Coord& coord, double z)
	: X (coord.x)
	, Y (coord.y)
	, Z (z)
{
}


Point3D::Point3D (const API_Coord3D& coord)
	: X (coord.x)
	, Y (coord.y)
	, Z (coord.z)
{
}


Point3D::Point3D (const Point3D& other)
	: X (other.X)
	, Y (other.Y)
	, Z (other.Z)
{
}


const API_Coord Point3D::ToAPI_Coord () const
{
	API_Coord coord;
	coord.x = X;
	coord.y = Y;

	return coord;
}


const API_Coord3D Point3D::ToAPI_Coord3D () const
{
	API_Coord3D coord;
	coord.x = X;
	coord.y = Y;
	coord.z = Z;

	return coord;
}


bool Point3D::operator == (const Point3D & rhs) const
{
	if (fabs (X - rhs.X) < EPS && fabs (Y - rhs.Y) < EPS && fabs (Z - rhs.Z) < EPS)
		return true;
	else
		return false;
}


Point3D& Point3D::operator = (const Point3D& other)
{
	if (this == &other) {
		return *this;
	}
	X = other.X;
	Y = other.Y;
	Z = other.Z;

	return *this;
}


GSErrCode Point3D::Restore (const GS::ObjectState& os)
{
	os.Get (XFieldName, X);
	os.Get (YFieldName, Y);
	os.Get (ZFieldName, Z);
	
	return NoError;
}


GSErrCode Point3D::Store (GS::ObjectState& os) const
{
	os.Add (XFieldName, X);
	os.Add (YFieldName, Y);
	os.Add (ZFieldName, Z);
	
	return NoError;
}


Point2D::Point2D ()
	: X (0.0)
	, Y (0.0)
{
}


Point2D::Point2D (double x, double y)
	: X (x)
	, Y (y)
{
}


Point2D::Point2D (const API_Coord& coord)
	: X (coord.x)
	, Y (coord.y)
{
}


const API_Coord Point2D::ToAPI_Coord () const
{
	API_Coord coord;
	coord.x = X;
	coord.y = Y;

	return coord;
}


bool Point2D::operator == (const Point2D & rhs) const
{
	if (X == rhs.X && Y == rhs.Y)
		return true;
	else
		return false;
}


GSErrCode Point2D::Restore (const GS::ObjectState& os)
{
	os.Get (XFieldName, X);
	os.Get (YFieldName, Y);
	
	return NoError;
}


GSErrCode Point2D::Store (GS::ObjectState& os) const
{
	os.Add (XFieldName, X);
	os.Add (YFieldName, Y);
	
	return NoError;
}