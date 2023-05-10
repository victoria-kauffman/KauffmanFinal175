#if !defined(FUNCTIONS_H)
#define FUNCTIONS_H

float2 cmult(float2 a, float2 b) {
    return float2(a.x * b.x - a.y * b.y, a.y * b.x + a.x * b.y);
}

#endif