#ifndef TEAM5_INDIVIDUALITY_COMMON_INCLUDED
#define TEAM5_INDIVIDUALITY_COMMON_INCLUDED

// 個性 0 → 0.25、0.5 → 0.7、1 → 1 をそれぞれ直線でつなぐ
half IndividualityOpacity(half individuality)
{
    half low = lerp(0.25, 0.7, saturate(individuality * 2.0));
    half high = lerp(0.7, 1.0, saturate(individuality * 2.0 - 1.0));
    return individuality < 0.5 ? low : high;
}

#endif
