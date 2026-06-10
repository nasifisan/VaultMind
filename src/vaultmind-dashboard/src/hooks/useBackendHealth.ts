"use client";

import { useState, useEffect } from "react";
import { checkHealth } from "../services/chatService.service";

/**
 * Custom hook to poll and monitor the backend API health status.
 */
export function useBackendHealth(pollIntervalMs = 150000): boolean {
  const [isOnline, setIsOnline] = useState<boolean>(true);

  useEffect(() => {
    checkHealth().then(setIsOnline);

    const interval = setInterval(() => {
      checkHealth().then(setIsOnline);
    }, pollIntervalMs);

    return () => clearInterval(interval);
  }, [pollIntervalMs]);

  return isOnline;
}
