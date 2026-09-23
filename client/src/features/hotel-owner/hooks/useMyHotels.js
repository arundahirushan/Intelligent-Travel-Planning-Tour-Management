import { useState, useEffect, useCallback } from 'react';
import { getMyHotels } from '../../../services/hotelOwnerApi';

export function useMyHotels(params = {}) {
  const [hotels, setHotels] = useState([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  // We serialize params to use it as a dependency for useEffect
  const serializedParams = JSON.stringify(params);

  const fetchHotels = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      // parse params back
      const currentParams = JSON.parse(serializedParams);
      const res = await getMyHotels(currentParams);
      
      setHotels(res.items || []);
      setTotalCount(res.totalCount || 0);
    } catch (err) {
      setError(err.response?.data?.message || err.message || 'Failed to fetch hotels');
    } finally {
      setLoading(false);
    }
  }, [serializedParams]);

  useEffect(() => {
    fetchHotels();
  }, [fetchHotels]);

  return { hotels, totalCount, loading, error, refetch: fetchHotels };
}
