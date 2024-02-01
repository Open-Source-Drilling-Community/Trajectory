# Conversion from Latitude-Longitude to X-Y

The earth is modelled as an oblate, i.e., a spheroid flatened at the pole. At a given latitude, a path on the Earth is a circle. Let us consider that the origin of 
longitudes is Greenwhich and that the Earth is modelled by a semi-long axis, $a$, and a flatening, $f$. The flatening is defined as:
$f = \frac{{a - b}}{{a}}$
where $b$ is the semi-short axis. Therefore the semi short axis can be expressed as:
$b = a - f \cdot a$

The radius of the Earth at a given latitude, $\phi$ is given by:
$R(\phi) = \frac{{a \cdot \sqrt{{\cos^2(\phi) + \frac{{b^2}}{{a^2}} \cdot \sin^2(\phi)}}}}{{\sqrt{1 - f \cdot (2 - f) \cdot \sin^2(\phi)}}}
$

<svg width="100" height="100">
    <ellipse cx="50" cy="50" rx="40" ry="30" stroke="black" stroke-width="3" fill="none" />
    <line x1="50" y1="0" x2="50" y2="100" stroke="black" stroke-width="1" stroke-dasharray="5,5"/>
    <ellipse cx="50" cy="35" rx="32" ry="10" stroke="black" stroke-width="1" stroke-dasharray="5,5" fill="none" />
    <ellipse cx="50" cy="50" rx="15" ry="30" stroke="black" stroke-width="1" stroke-dasharray="5,5" fill="none" />
    <line x1="50" y1="50" x2="60" y2="45" stroke="black" stroke-width="2" />
</svg>

At that latitude the $y$-coordinate (east-west) is the length of the circular arc counted from the Greenwich meridian, i.e., the longitude angle, $\lambda$:
$y = R(\phi) \cdot \lambda$
The $x$-coordinate (south-north) is the length of the elliptical arc counted from the equator using the latitude. This involves the elliptic integral of the second
kind, denoted $E(\phi, m)$ where $m=1- \frac{{b^2}}{{a^2}}$. Its definition is:
$E(\phi, m) = \int_0^\phi \sqrt{1 - m \cdot \sin^2(t)} \, dt$

The definition of $x$ is then:
$x = a \cdot E(\phi, m)$
Conversely, to retrieve the latitude and longitude from the $x$ and $y$ coordinates, i.e., arc lengths, the following method is used:
$\phi = E^{-1}(\frac{x}{a}, m)$

and 
$\lambda = \frac{y}{R(\phi)}$


